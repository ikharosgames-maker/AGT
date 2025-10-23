using Agt.Desktop.Services; // IBlockLibrary
using Agt.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    public partial class StartCaseDialog : Window
    {
        // --- DTO pro verze v ComboBoxu ---
        private sealed class VerOpt
        {
            public Guid Id { get; set; }            // FormVersionId
            public Guid FormId { get; set; }
            public string FormName { get; set; } = "";
            public string Version { get; set; } = ""; // "0.1.0"
            public Version SortVer { get; set; } = new Version(0, 0, 0, 0);
            public string Display { get; set; } = "";
            public string FvPath { get; set; } = "";  // cesta k form-version souboru
            public string LayoutPath { get; set; } = ""; // layouts/{Id}.json
        }

        // --- DTO pro položky pinů v ListBoxu ---
        private sealed class StartBlockOption
        {
            public Guid BlockId { get; set; }
            public string Version { get; set; } = "";
            public string Display { get; set; } = "";  // "Název  vX.Y.Z" (fallback GUID)
            public bool IsChecked { get; set; } = true;
        }

        // --- JSON tvar pinů (uvnitř BlockPinsJson) ---
        private sealed class Pin
        {
            public Guid BlockId { get; set; }
            public string Version { get; set; } = "";
        }

        private List<StartBlockOption> _blockOptions = new();

        public Guid SelectedFormVersionId { get; private set; }
        // zachovávám název property – vrací GUIDy jako stringy
        public IReadOnlyList<string> SelectedStartBlockKeys { get; private set; } = Array.Empty<string>();

        public StartCaseDialog()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadPublishedVersionsAsync();
        }

        // === Cesty do běhového repozitáře ===
        private static string Dir(string name)
        {
            try
            {
                var t = Type.GetType("Agt.Infrastructure.JsonStore.JsonPaths, Agt.Infrastructure");
                var mi = t?.GetMethod("Dir", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var r = mi?.Invoke(null, new object?[] { name }) as string;
                if (!string.IsNullOrWhiteSpace(r)) return r!;
            }
            catch { }
            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", name);
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static Version ParseSemVer(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Version(0, 0, 0, 0);
            var t = s.Trim().TrimStart('v', 'V');
            // zkus 3 segmenty, případně doplň nuly
            var parts = t.Split('.');
            while (parts.Length < 3) t += ".0";
            Version.TryParse(t, out var v);
            return v ?? new Version(0, 0, 0, 0);
        }

        private async Task LoadPublishedVersionsAsync()
        {
            try
            {
                var options = await Task.Run(() =>
                {
                    var formsDir = Dir("forms");
                    var versDir = Dir("form-versions");
                    var layoutsDir = Dir("layouts");

                    // 1) FormId -> (Name, CurrentVersionId)
                    var formsMeta = new Dictionary<Guid, (string Name, Guid CurrentVer)>();
                    foreach (var f in Directory.EnumerateFiles(formsDir, "*.json"))
                    {
                        if (!Guid.TryParse(Path.GetFileNameWithoutExtension(f), out var fid)) continue;
                        try
                        {
                            var jo = JsonNode.Parse(File.ReadAllText(f)) as JsonObject;
                            var name = jo?["Name"]?.ToString() ?? jo?["Key"]?.ToString() ?? fid.ToString();
                            var curStr = jo?["CurrentVersionId"]?.ToString();
                            Guid.TryParse(curStr, out var curVer);
                            formsMeta[fid] = (name ?? fid.ToString(), curVer);
                        }
                        catch { /* ignore */ }
                    }

                    // 2) kandidáti: Published + existující layout {FormVersionId}.json
                    var candidates = new List<VerOpt>();
                    foreach (var fvPath in Directory.EnumerateFiles(versDir, "*.json"))
                    {
                        try
                        {
                            var jo = JsonNode.Parse(File.ReadAllText(fvPath)) as JsonObject;
                            if (jo == null) continue;

                            // FormVersionId může být v "Id" nebo "FormVersionId"
                            var formVersionId = TryGetGuid(jo, "Id") ?? TryGetGuid(jo, "FormVersionId");
                            if (formVersionId == null) continue;

                            var formId = TryGetGuid(jo, "FormId");
                            if (formId == null) continue;

                            var version = jo["Version"]?.ToString() ?? "";

                            // Published? (podpora int i string)
                            var statusOk = false;
                            if (jo["Status"] is JsonValue sv)
                            {
                                if (sv.TryGetValue<int>(out var si)) statusOk = (si == (int)FormStatus.Published);
                                else statusOk = sv.ToString().Equals("Published", StringComparison.OrdinalIgnoreCase);
                            }
                            if (!statusOk) continue;

                            // layout je pojmenovaný podle FormVersionId (GUID)
                            var layoutPath = Path.Combine(layoutsDir, formVersionId.Value.ToString("D") + ".json");
                            if (!File.Exists(layoutPath)) continue;

                            // jméno formuláře z formsMeta (pokud ho známe)
                            var displayName = formsMeta.TryGetValue(formId.Value, out var meta)
                                              ? meta.Name
                                              : formId.Value.ToString();

                            candidates.Add(new VerOpt
                            {
                                Id = formVersionId.Value,   // FormVersionId
                                FormId = formId.Value,
                                FormName = displayName,
                                Version = version,
                                SortVer = ParseSemVer(version),
                                Display = $"{displayName}  v{version}",
                                FvPath = fvPath,
                                LayoutPath = layoutPath
                            });
                        }
                        catch { /* ignore */ }
                    }

                    // 3) nech jen aktuální verze (CurrentVersionId) pro daný Form
                    var currentOnly = candidates
                        .Where(c => formsMeta.TryGetValue(c.FormId, out var fm) && fm.CurrentVer == c.Id)
                        .ToList();

                    // 4) řazení podle jména formuláře
                    return currentOnly
                        .OrderBy(o => o.FormName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                });

                VersionBox.ItemsSource = options;

                if (VersionBox.Items.Count == 0)
                {
                    MessageBox.Show("Nebyly nalezeny žádné aktuální publikované verze (s existujícím layoutem).",
                                    "Spustit případ", MessageBoxButton.OK, MessageBoxImage.Information);
                    BlocksList.ItemsSource = null;
                    _blockOptions.Clear();
                    return;
                }

                VersionBox.SelectedIndex = 0;
                ReloadPins();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Načítání verzí selhalo: " + ex.Message,
                                "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static Guid? TryGetGuid(JsonObject jo, string prop)
        {
            var s = jo[prop]?.ToString();
            return Guid.TryParse(s, out var g) ? g : (Guid?)null;
        }

        private void VersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadPins();
        }

        private void ReloadPins()
        {
            BlocksList.ItemsSource = null;
            _blockOptions.Clear();

            if (VersionBox.SelectedItem is not VerOpt opt) return;

            try
            {
                // otevři form-version JSON a vytáhni BlockPinsJson
                var fvJo = JsonNode.Parse(File.ReadAllText(opt.FvPath)) as JsonObject;
                if (fvJo == null)
                {
                    MessageBox.Show("Soubor verze nelze načíst.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var pinsJson = fvJo["BlockPinsJson"]?.ToString();
                if (string.IsNullOrWhiteSpace(pinsJson))
                {
                    MessageBox.Show("Publikovaná verze neobsahuje BlockPinsJson.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var pins = JsonSerializer.Deserialize<List<Pin>>(pinsJson) ?? new();

                // názvy bloků jen pro zobrazení (z knihovny)
                var lib = Agt.Desktop.App.Services?.GetService(typeof(IBlockLibrary)) as IBlockLibrary;
                var entries = (lib?.Enumerate() ?? Enumerable.Empty<BlockLibEntry>()).ToList();
                var nameBy = entries
                    .GroupBy(e => (e.BlockId, e.Version))
                    .ToDictionary(g => g.Key, g => g.First().Name);

                _blockOptions = pins.Select(p =>
                {
                    nameBy.TryGetValue((p.BlockId, p.Version ?? ""), out var name);
                    var disp = !string.IsNullOrWhiteSpace(name) ? name! : p.BlockId.ToString("D");
                    return new StartBlockOption
                    {
                        BlockId = p.BlockId,
                        Version = p.Version ?? "",
                        Display = $"{disp}  v{(p.Version ?? "")}",
                        IsChecked = true
                    };
                }).ToList();

                BlocksList.ItemsSource = _blockOptions;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Načítání bloků selhalo: " + ex.Message,
                                "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // uvnitř třídy StartCaseDialog
        public string? SelectedLayoutPath { get; private set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (VersionBox.SelectedItem is not VerOpt opt)
            {
                DialogResult = false;
                return;
            }

            // poslední kontrola integrity – layout musí existovat
            if (!File.Exists(opt.LayoutPath))
            {
                MessageBox.Show("Pro vybranou verzi chybí layout. Zkuste jinou verzi nebo publikujte znovu.",
                                "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                return;
            }

            SelectedFormVersionId = opt.Id;

            var selected = _blockOptions
                .Where(o => o.IsChecked)
                .Select(o => o.BlockId.ToString("D"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            SelectedStartBlockKeys = selected;

            // >>> NOVÉ: ulož přesnou cestu layoutu, ať ji použije runner beze sporu
            SelectedLayoutPath = opt.LayoutPath;

            DialogResult = true;
        }
    }
}
