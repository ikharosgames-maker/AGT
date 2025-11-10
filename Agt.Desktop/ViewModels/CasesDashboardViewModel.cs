using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agt.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel pro přehled case na hlavním okně (dashboard).
    /// Sloupce: Formulář, Založeno, Založil, Krok (i/N), Aktuální stage.
    /// </summary>
    public sealed class CasesDashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<CaseRow> Items { get; } = new ObservableCollection<CaseRow>();

        private CaseRow? _selected;
        public CaseRow? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public CasesDashboardViewModel()
        {
            try
            {
                var disp = Agt.Desktop.App.Current?.Dispatcher;
                if (disp == null || disp.CheckAccess())
                    Load();
                else
                    disp.BeginInvoke(new Action(Load));
            }
            catch
            {
                // fallback: kdyby selhal dispatcher, stejně to zkusíme
                Load();
            }
        }

        private void Load()
        {
            Cases.Clear();
            var data = _casesRepo.ListRecent(500).ToList();

            foreach (var c in data)
            {
                var blocks = _casesRepo.ListBlocks(c.Id).ToList();
                var openBlocks = blocks.Where(b => b.State is CaseBlockState.Open).ToList();

                Cases.Add(new CaseRow
                {
                    Id = c.Id,
                    FormVersion = c.FormVersionId.ToString().Substring(0, 8),
                    StartedBy = c.StartedBy.ToString().Substring(0, 8),
                    StartedAt = c.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    OpenBlocks = openBlocks.Count,
                    Assignees = string.Join(",",
                        openBlocks.Select(b => b.AssigneeUserId is null ? "—" : b.AssigneeUserId.Value.ToString().Substring(0, 8))),
                    DueAt = openBlocks.Min(b => b.DueAt)?.ToString("yyyy-MM-dd") ?? "",
                    Status = openBlocks.Any() ? "Otevřený" : "V procesu/Locked"
                });
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var ft = FilterText.Trim().ToLowerInvariant();
                var filtered = Cases.Where(r =>
                    r.Id.ToString().ToLower().Contains(ft) ||
                    r.FormVersion.ToLower().Contains(ft) ||
                    r.StartedBy.ToLower().Contains(ft)).ToList();

                Cases.Clear();
                foreach (var r in filtered) Cases.Add(r);
            }
        }

        private async void StartNewCase()
        {
            var win = new Agt.Desktop.Views.StartCaseDialog
            {
                Owner = Agt.Desktop.App.Current.MainWindow
            };
            if (win.ShowDialog() != true) return;

            var formVersionId = win.SelectedFormVersionId;

            // očekávané umístění layoutu: %AppData%\AGT\layouts\{FormVersionId}.json
            var layoutPath = !string.IsNullOrWhiteSpace(win.SelectedLayoutPath)
                ? win.SelectedLayoutPath
                : System.IO.Path.Combine(GetDir("layouts"), formVersionId.ToString("D") + ".json");

            if (!File.Exists(layoutPath))
            {
                MessageBox.Show($"Pro vybranou verzi nebyl nalezen layout:\n{layoutPath}\n\nPublikuj formulář znovu.",
                                "Spustit případ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1) normalizuj vybrané bloky na GUID "D"
            var blocks = (win.SelectedStartBlockKeys ?? Array.Empty<string>())
                .Select(s => Guid.Parse(s).ToString("D"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // 2) failsafe – když si uživatel nic nezaškrtne, vem bloky z layoutu (první stage)
            if (blocks.Length == 0)
                blocks = ResolveInitialBlocksFromLayoutFile(layoutPath);

            if (blocks.Length == 0)
            {
                MessageBox.Show("Pro tuto verzi formuláře se nepodařilo určit počáteční bloky.",
                    "Spustit případ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                var actor = Guid.Parse("00000000-0000-0000-0000-000000000001");

                var caseId = await System.Threading.Tasks.Task.Run(() =>
                    _caseService.StartCase(formVersionId, actor, new StartSelection(null, blocks)));

                Load(); // refresh dashboard

                var run = new Agt.Desktop.Views.StageRunWindow(formVersionId, blocks, caseId)
                {
                    Owner = Agt.Desktop.App.Current.MainWindow
                };
                run.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Start případu selhal: " + ex.Message, "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // === pomocné funkce (pokud je už někde máš, použij svoje) ===
        private static string GetDir(string name)
        {
            try
            {
                var t = Type.GetType("Agt.Infrastructure.JsonStore.JsonPaths, Agt.Infrastructure");
                var mi = t?.GetMethod("Dir", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var r = mi?.Invoke(null, new object?[] { name }) as string;
                if (!string.IsNullOrWhiteSpace(r))
                {
                    Directory.CreateDirectory(r!);
                    return r!;
                }
            }
            catch { }
            var fallback = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", name);
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static string[] ResolveInitialBlocksFromLayoutFile(string layoutPath)
        {
            try
            {
                var json = File.ReadAllText(layoutPath);
                var root = JsonNode.Parse(json) as JsonObject;
                if (root is null) return Array.Empty<string>();

                var stages = root["Stages"] as JsonArray;
                var blocks = root["Blocks"] as JsonArray;
                if (stages is null || stages.Count == 0 || blocks is null) return Array.Empty<string>();

                var firstStageId = stages[0] is JsonObject st ? st["Id"]?.ToString() : null;
                if (string.IsNullOrWhiteSpace(firstStageId)) return Array.Empty<string>();

                return blocks
                    .OfType<JsonObject>() // << opraveno: generická verze
                    .Where(b => string.Equals(b["StageId"]?.ToString(), firstStageId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(b => TryDouble(b, "Y") ?? 0)
                    .ThenBy(b => TryDouble(b, "X") ?? 0)
                    .Select(b => b["BlockId"]?.ToString())
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(s => Guid.Parse(s!).ToString("D"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }

            static double? TryDouble(JsonObject o, string name)
            {
                var v = o[name];
                if (v is JsonValue val)
                {
                    if (val.TryGetValue<double>(out var d)) return d;
                    if (double.TryParse(val.ToString(),
                                        NumberStyles.Any,
                                        CultureInfo.InvariantCulture,
                                        out var d2)) return d2;
                }
                return null;
            }
        }

        private void OpenSelected()
        {
            MessageBox.Show("TODO: otevřít detail vybraného Case (nové okno).");
        }

        private void CompleteSelectedBlock()
        {
            MessageBox.Show("TODO: vybrat blok a zavolat ICaseService.CompleteBlock(...).");
        }

        private void ReopenSelectedBlock()
        {
            MessageBox.Show("TODO: vybrat blok a ICaseService.ReopenBlock(...).");
        }

    }

    public sealed class CaseRow
    {
        public Guid Id { get; set; }
        public string FormVersion { get; set; } = "";
        public string StartedBy { get; set; } = "";
        public string StartedAt { get; set; } = "";
        public int OpenBlocks { get; set; }
        public string Assignees { get; set; } = "";
        public string DueAt { get; set; } = "";
        public string Status { get; set; } = "";
    }
}