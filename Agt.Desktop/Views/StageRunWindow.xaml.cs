using Agt.Desktop.Models; // FieldComponentBase + konkrétní typy + RenderMode
using Agt.Domain.Models;
using Agt.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DomainCaseDataSnapshot = Agt.Domain.Models.CaseDataSnapshot;

namespace Agt.Desktop.Views
{
    public partial class StageRunWindow : Window
    {
        private readonly Guid _formVersionId;
        private readonly IServiceProvider _sp;
        private Guid _caseId;

        private LayoutSnapshot _layout = new();
        private RunStageVm _vm = new();

        private Guid _currentStageId = Guid.Empty;
        private bool _isCaseReadOnly = false;

        // ---------- DEBUG LOG ----------
        private static string? _activeLogPath;
        private static readonly List<string> _logCandidates = new();

        static StageRunWindow()
        {
            try { var p = Agt.Infrastructure.JsonStore.JsonPaths.Dir("logs"); Directory.CreateDirectory(p); _logCandidates.Add(Path.Combine(p, "StageRun.log")); } catch { }
            try { var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "agt", "logs"); Directory.CreateDirectory(p); _logCandidates.Add(Path.Combine(p, "StageRun.log")); } catch { }
            try { var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "logs"); Directory.CreateDirectory(p); _logCandidates.Add(Path.Combine(p, "StageRun.log")); } catch { }
            foreach (var p in _logCandidates) { try { File.AppendAllText(p, ""); _activeLogPath = p; break; } catch { } }
            SafeLog("===== StageRunWindow session " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " =====");
            SafeLog("Log candidates: " + string.Join(" | ", _logCandidates));
            SafeLog("Active log: " + (_activeLogPath ?? "(none)"));
        }

        private static void SafeLog(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss.fff} [StageRun] {message}";
            try { Debug.WriteLine(line); } catch { }
            try { Console.WriteLine(line); } catch { }
            if (_activeLogPath != null) { try { File.AppendAllText(_activeLogPath, line + Environment.NewLine, Encoding.UTF8); } catch { } }
        }
        // -----------------------------------

        public StageRunWindow(Guid formVersionId, IEnumerable<string>? startInstanceKeys = null, Guid? caseId = null)
        {
            InitializeComponent();

            if (formVersionId == Guid.Empty)
                throw new ArgumentException("formVersionId nesmí být Guid.Empty", nameof(formVersionId));

            _formVersionId = formVersionId;
            _sp = Agt.Desktop.App.Services;
            _caseId = caseId ?? Guid.Empty;

            // Header
            var forms = _sp.GetRequiredService<IFormRepository>();
            var fv = forms.GetVersion(formVersionId);
            string header = "Běh případu";
            if (fv != null)
            {
                var f = forms.Get(fv.FormId);
                header = f != null ? $"{f.Name}  v{fv.Version}" : $"Form v{fv.Version}";
            }
            if (_caseId != Guid.Empty) header += $"  |  Case: {_caseId.ToString()[..8]}";
            HeaderTitle.Text = header;

            // Layout
            _layout = LoadLayout(formVersionId);

            // RO? -> žádný OPEN blok v case
            if (_caseId != Guid.Empty)
            {
                try
                {
                    var repo = _sp.GetService<ICaseRepository>();
                    var blocks = repo?.ListBlocks(_caseId)?.ToList() ?? new List<CaseBlock>();
                    _isCaseReadOnly = !blocks.Any(b => b.State == CaseBlockState.Open);
                }
                catch { }
            }

            // vyber stage (preferuj stage s otevřenými bloky, jinak první)
            _currentStageId = SelectStageId(startInstanceKeys);

            // Naplň VM – chronologicky shora dolů; paralely vedle sebe
            BuildStageVm(_currentStageId);
            DataContext = _vm;

            var st = _layout.Stages.FirstOrDefault(s => s.Id == _currentStageId);
            StageInfo.Text = st != null ? $"Stage: {st.Title ?? st.Id.ToString()[..8]}" : "Stage";

            // načti case data (doplní se i do RO bloků)
            if (_caseId != Guid.Empty)
                _ = LoadAndApplyCaseDataAsync(_caseId);

            // 🔸 po vytvoření vizuálu přenastavíme interaktivitu dle Mode
            Loaded += StageRunWindow_Loaded;
        }

        // ----------------------- UI akce -----------------------
        private void StageRunWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // (1) interaktivita dle Mode (RO vs Run)
                foreach (var viewer in FindVisualChildren<Agt.Desktop.Views.ComponentViewerControl>(this))
                {
                    if (viewer.DataContext is RunBlockVm rb)
                        viewer.IsEnabled = rb.Mode == RenderMode.Run && !_isCaseReadOnly;
                }

                // (2) sjednocení paddingu uvnitř vieweru na 8 (jako v editoru)
                foreach (var asc in FindVisualChildren<Agt.Desktop.Views.AutoSizeCanvas>(this))
                    asc.Padding = new Thickness(8);

                // (3) zarovnání headeru: první TextBlock uvnitř bloku → Margin = (8,0,8,8)
                //    (bez hrabání do Border.Padding, ať nerozhodíme celkovou metriku)
                foreach (var cp in FindVisualChildren<ContentPresenter>(this))
                {
                    if (cp.DataContext is RunBlockVm)
                    {
                        var tb = FindFirstChild<TextBlock>(cp);
                        if (tb != null)
                            tb.Margin = new Thickness(8, 0, 8, 8);
                    }
                }
            }
            catch { /* best-effort */ }
        }
        private static T? FindFirstChild<T>(DependencyObject root) where T : DependencyObject
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) return t;
                var deeper = FindFirstChild<T>(child);
                if (deeper is not null) return deeper;
            }
            return null;
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) yield break;
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;
                foreach (var sub in FindVisualChildren<T>(child)) yield return sub;
            }
        }

        private async void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (_isCaseReadOnly)
            {
                MessageBox.Show("Tento případ je již dokončen. Je otevřen v režimu pouze pro čtení.", "Read-only", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_caseId == Guid.Empty)
                {
                    _caseId = Guid.NewGuid();
                    if (!HeaderTitle.Text.Contains("Case:"))
                        HeaderTitle.Text += $"  |  Case: {_caseId.ToString()[..8]}";
                }

                var snap = BuildSnapshotFromVm(_caseId, _formVersionId);

                var repo = _sp.GetService<ICaseDataRepository>()
                          ?? throw new InvalidOperationException("ICaseDataRepository není zaregistrováno (DI).");

                await repo.SaveAsync(snap);

                MessageBox.Show("Uloženo.", "Uložení", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uložení selhalo: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CompleteStage_Click(object? sender, RoutedEventArgs e)
        {
            if (_isCaseReadOnly)
            {
                MessageBox.Show("Případ je dokončen (read-only).", "Read-only", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (_caseId == Guid.Empty)
                    _caseId = Guid.NewGuid();

                var snap = BuildSnapshotFromVm(_caseId, _formVersionId);

                var repo = _sp.GetService<ICaseDataRepository>()
                          ?? throw new InvalidOperationException("ICaseDataRepository není zaregistrováno.");

                await repo.SaveAsync(snap);

                await TryAdvanceCaseAsync(_caseId, _currentStageId);

                // přehodnotit RO
                try
                {
                    var repo2 = _sp.GetService<ICaseRepository>();
                    _isCaseReadOnly = !(repo2?.ListBlocks(_caseId)?.Any(b => b.State == CaseBlockState.Open) ?? false);
                }
                catch { }

                StageRunUiBus.PublishCaseChanged(_caseId);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Dokončení kroku selhalo: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object? sender, RoutedEventArgs e) => Close();

        // ----------------------- Async načtení dat case -----------------------

        private async Task LoadAndApplyCaseDataAsync(Guid caseId)
        {
            try
            {
                var repo = _sp.GetService<ICaseDataRepository>();
                if (repo == null) return;

                var snap = await repo.LoadAsync(caseId);
                if (snap?.Values != null && snap.Values.Count > 0)
                    ApplyValuesToVm(snap.Values);
            }
            catch
            {
                // tichý fallback – UI neblokovat
            }
        }

        // ----------------------- VM naplnění -----------------------

        private Guid SelectStageId(IEnumerable<string>? startInstanceKeys)
        {
            if (_caseId != Guid.Empty)
            {
                try
                {
                    var cases = _sp.GetService<ICaseRepository>();
                    var blocks = cases?.ListBlocks(_caseId)?.ToList() ?? new List<CaseBlock>();

                    var openStageIds = blocks
                        .Where(b => b.State == CaseBlockState.Open)
                        .Select(b => _layout.Blocks.FirstOrDefault(bl => bl.BlockId == b.BlockDefinitionId)?.StageId ?? Guid.Empty)
                        .Where(id => id != Guid.Empty)
                        .Distinct()
                        .ToList();

                    if (openStageIds.Count > 0)
                    {
                        var pick = _layout.Stages
                            .Where(s => openStageIds.Contains(s.Id))
                            .OrderBy(s => s.X).ThenBy(s => s.Y)
                            .Select(s => s.Id)
                            .First();
                        return pick;
                    }

                    if (_isCaseReadOnly)
                    {
                        var last = _layout.Stages.OrderBy(s => s.X).ThenBy(s => s.Y).LastOrDefault()?.Id ?? Guid.Empty;
                        if (last != Guid.Empty) return last;
                    }
                }
                catch { }
            }

            if (startInstanceKeys != null)
            {
                foreach (var k in startInstanceKeys)
                {
                    if (Guid.TryParse(k, out var blockDefId))
                    {
                        var st = _layout.Blocks.FirstOrDefault(b => b.BlockId == blockDefId)?.StageId ?? Guid.Empty;
                        if (st != Guid.Empty) return st;
                    }
                }
            }
            return _layout.Stages.OrderBy(s => s.X).ThenBy(s => s.Y).FirstOrDefault()?.Id ?? Guid.Empty;
        }

        private void BuildStageVm(Guid currentStageId)
        {
            _vm = new RunStageVm();
            if (currentStageId == Guid.Empty) { DataContext = _vm; return; }

            var orderedStages = _layout.Stages.OrderBy(s => s.X).ThenBy(s => s.Y).ToList();
            var currentIndex = Math.Max(0, orderedStages.FindIndex(s => s.Id == currentStageId));
            var visibleStages = _isCaseReadOnly ? orderedStages
                                                : orderedStages.Take(currentIndex + 1).ToList();

            // Jediná “pravda” o okrajích:
            // - AutoSizeCanvas má Padding=8 (nastavujeme v Loaded)
            // - rám stage má vlastní vnitřní padding (StageInnerPadding)
            const double StageOuterPadding = 8;     // vnější okraj kolem celé stage
            const double StageInnerPadding = 8;     // vnitřní okraj uvnitř rámu (kolem bloků)
            const double ColGap = 12;    // mezera mezi bloky v rámci stage
            const double StageBetweenGap = 40;    // mezera mezi stage (malinko větší)
            const double EmptyRowMinHeight = 40;    // minimální výška prázdné stage

            double y = StageOuterPadding;


            foreach (var stage in visibleStages)
            {
                // 1) připrav bloky v této stage (rozměr = čistý obsah; plný rozměr dopočítá AutoSizeCanvas)
                var blockLayouts = _layout.Blocks
                    .Where(b => b.StageId == stage.Id)
                    .OrderBy(b => b.Y).ThenBy(b => b.X)
                    .ToList();

                var rowBlocks = new List<RunBlockVm>();
                foreach (var b in blockLayouts)
                {
                    string schemaJson = !string.IsNullOrWhiteSpace(b.SchemaJson)
                        ? b.SchemaJson!
                        : (LoadBlockSchemaJson(b.BlockId, b.Version) ?? string.Empty);

                    var rb = new RunBlockVm
                    {
                        BlockId = b.BlockId,
                        InstanceKey = b.BlockId != Guid.Empty ? b.BlockId.ToString("D")
                                    : (!string.IsNullOrWhiteSpace(b.LegacyInstanceKey) ? b.LegacyInstanceKey!
                                    : (b.LegacyDefKey ?? string.Empty)),
                        Title = !string.IsNullOrWhiteSpace(b.Title) ? b.Title!
                               : (!string.IsNullOrWhiteSpace(b.LegacyDefKey) ? b.LegacyDefKey!
                               : (!string.IsNullOrWhiteSpace(b.LegacyInstanceKey) ? b.LegacyInstanceKey!
                               : (b.BlockId != Guid.Empty ? b.BlockId.ToString("D") : "Block"))),
                        Mode = _isCaseReadOnly
                                ? RenderMode.ReadOnly
                                : (stage.Id == currentStageId ? RenderMode.Run : RenderMode.ReadOnly)
                    };

                    if (string.IsNullOrWhiteSpace(schemaJson))
                    {
                        // fallback na uložené rozměry bloku (bez jakéhokoli globálního paddingu)
                        rb.PreviewWidth = Math.Max(1, b.Width);
                        rb.PreviewHeight = Math.Max(1, b.Height);
                    }
                    else
                    {
                        FillComponentsFromSchema(rb, schemaJson); // pro runtime vykreslení
                    }

                    rowBlocks.Add(rb);
                }

                // 2) metrika obsahu stage: titulek + jeho spodní margin + viewer (obsah)
                double maxViewerH = rowBlocks.Count > 0 ? rowBlocks.Max(r => r.PreviewHeight) : 0;

                double rowHeight = rowBlocks.Count > 0
                    ? maxViewerH
                    : EmptyRowMinHeight;

                double contentWidth = rowBlocks.Count > 0
                    ? rowBlocks.Sum(r => r.PreviewWidth) + ColGap * (rowBlocks.Count - 1)
                    : 0;

                // 3) rám stage – přidáváme jen vlastní StageInnerPadding (canvas padding už nepřičítáme!)
                double frameWidth = StageInnerPadding + contentWidth + StageInnerPadding;
                double frameHeight = StageInnerPadding + rowHeight + StageInnerPadding;

                var frame = new RunBlockVm
                {
                    BlockId = Guid.Empty,
                    InstanceKey = $"__stage_frame__:{stage.Id}",
                    Title = "",
                    Mode = RenderMode.ReadOnly,
                    PreviewWidth = frameWidth,
                    PreviewHeight = frameHeight,
                    X = StageOuterPadding,
                    Y = y
                };
                _vm.Blocks.Add(frame);

                // 4) bloky do rámu
                double x = StageOuterPadding + StageInnerPadding;
                double blocksTop = y + StageInnerPadding;
                foreach (var rb in rowBlocks)
                {
                    rb.X = x;
                    rb.Y = blocksTop;
                    _vm.Blocks.Add(rb);
                    x += rb.PreviewWidth + ColGap;
                }

                // 5) posun na další stage
                y += frameHeight + StageOuterPadding + StageBetweenGap;
            }

            DataContext = _vm;
        }
        private static double MeasureTextHeight(string text, string fontFamily, double fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var tf = new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
            var ft = new FormattedText(text,
                                       CultureInfo.CurrentUICulture,
                                       FlowDirection.LeftToRight,
                                       tf,
                                       fontSize,
                                       Brushes.Black,
                                       1.0);
            return ft.Height;
        }
        // ----------------------- Layout load -----------------------
        private static LayoutSnapshot LoadLayout(Guid formVersionId)
        {
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("layouts");
            Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, formVersionId + ".json");
            var result = new LayoutSnapshot { SourcePath = path };

            if (!File.Exists(path)) return result;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;

                if (root.TryGetProperty("Stages", out var sEl) && sEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var st in sEl.EnumerateArray())
                    {
                        result.Stages.Add(new StageLayout
                        {
                            Id = st.GetProperty("Id").GetGuid(),
                            Title = TryGetString(st, "Title"),
                            X = TryGetDouble(st, "X", 0),
                            Y = TryGetDouble(st, "Y", 0),
                            Width = TryGetDouble(st, "Width", 600),
                            Height = TryGetDouble(st, "Height", 400)
                        });
                    }
                }

                if (root.TryGetProperty("Blocks", out var bEl) && bEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bl in bEl.EnumerateArray())
                    {
                        result.Blocks.Add(new BlockLayout
                        {
                            BlockId = TryGetGuid(bl, "BlockId"),
                            LegacyInstanceKey = TryGetString(bl, "InstanceKey"),
                            LegacyDefKey = TryGetString(bl, "DefKey"),
                            Version = TryGetString(bl, "Version") ?? "1.0.0",
                            Title = TryGetString(bl, "Title"),
                            StageId = bl.GetProperty("StageId").GetGuid(),
                            X = TryGetDouble(bl, "X", 0),
                            Y = TryGetDouble(bl, "Y", 0),
                            Width = TryGetDouble(bl, "Width", 320),
                            Height = TryGetDouble(bl, "Height", 200),
                            SchemaJson = TryGetString(bl, "SchemaJson")
                        });
                    }
                }

                return result;
            }
            catch
            {
                return result;
            }
        }

        private static Guid TryGetGuid(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v)
                ? (v.ValueKind == JsonValueKind.String && Guid.TryParse(v.GetString(), out var g) ? g
                    : v.ValueKind == JsonValueKind.Object && v.TryGetProperty("Id", out var idEl) && Guid.TryParse(idEl.ToString(), out var g2) ? g2
                    : v.ValueKind == JsonValueKind.Undefined ? Guid.Empty
                    : v.GetGuid())
                : Guid.Empty;

        // ----------------------- Schema → komponenty -----------------------
        private static void FillComponentsFromSchema(RunBlockVm b, string schemaJson)
        {
            b.Components.Clear();
            if (string.IsNullOrWhiteSpace(schemaJson)) return;

            try
            {
                using var doc = JsonDocument.Parse(schemaJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("Items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                    return;

                foreach (var it in itemsEl.EnumerateArray())
                {
                    double x = TryGetDouble(it, "X", 0), y = TryGetDouble(it, "Y", 0);
                    double w = TryGetDouble(it, "Width", 120), h = TryGetDouble(it, "Height", 28);
                    int z = (int)TryGetDouble(it, "ZIndex", 0);

                    string typeKey = (TryGetString(it, "TypeKey") ?? "").Trim().ToLowerInvariant();
                    string label = TryGetString(it, "Label") ?? "";
                    string fieldKey = TryGetString(it, "FieldKey") ?? "";
                    string name = TryGetString(it, "Name") ?? "";
                    string? defVal = TryGetString(it, "DefaultValue");

                    FieldComponentBase comp = typeKey switch
                    {
                        "label" => new LabelField(),
                        "textbox" => new TextBoxField(),
                        "textarea" => new TextAreaField(),
                        "number" => new NumberField(),
                        "date" => new DateField(),
                        "combobox" => new ComboBoxField(),
                        "checkbox" => new CheckBoxField(),
                        _ => new LabelField()
                    };

                    TrySet(comp, "Label", label);
                    TrySet(comp, "FieldKey", fieldKey);
                    TrySet(comp, "Name", name);
                    if (!string.IsNullOrWhiteSpace(defVal))
                        TrySet(comp, "Value", defVal);

                    comp.X = x;
                    comp.Y = y;
                    comp.ZIndex = z;

                    TrySet(comp, "Width", w);
                    TrySet(comp, "Height", h);

                    double totalW = w, totalH = h;
                    switch (typeKey)
                    {
                        case "textbox":
                        case "textarea":
                        case "number":
                        case "date":
                        case "combobox":
                            totalW = w + 2;     // border L/R
                            totalH = h + 2;     // border T/B
                            break;
                        case "checkbox":
                            double textW = MeasureTextWidth(label, "Segoe UI", 12);
                            totalW = Math.Max(w, 20 + 4 + textW);
                            totalH = Math.Max(h, 20);
                            break;
                        default:
                            totalW = w; totalH = h;
                            break;
                    }

                    TrySet(comp, "TotalWidth", totalW);
                    TrySet(comp, "TotalHeight", totalH);

                    b.Components.Add(comp);
                }
            }
            catch
            {
                // ignore
            }
        }

        private static (double itemsW, double itemsH) MeasureSchemaForPreview(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson)) return (1, 1);
            try
            {
                using var doc = JsonDocument.Parse(schemaJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("Items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                    return (1, 1);

                double maxRight = 0, maxBottom = 0;
                foreach (var it in itemsEl.EnumerateArray())
                {
                    var x = TryGetDouble(it, "X", 0);
                    var y = TryGetDouble(it, "Y", 0);
                    var w = TryGetDouble(it, "Width", 120);
                    var h = TryGetDouble(it, "Height", 28);
                    var typeKey = (TryGetString(it, "TypeKey") ?? "").Trim().ToLowerInvariant();

                    double totalW = w, totalH = h;
                    switch (typeKey)
                    {
                        case "textbox":
                        case "textarea":
                        case "number":
                        case "date":
                        case "combobox":
                            totalW = w + 2;
                            totalH = h + 2;
                            break;
                        case "checkbox":
                            double textW = MeasureTextWidth(TryGetString(it, "Label") ?? "", "Segoe UI", 12);
                            totalW = Math.Max(w, 20 + 4 + textW);
                            totalH = Math.Max(h, 20);
                            break;
                    }

                    maxRight = Math.Max(maxRight, x + totalW);
                    maxBottom = Math.Max(maxBottom, y + totalH);
                }

                return (Math.Max(1, Math.Ceiling(maxRight) + 1),
                        Math.Max(1, Math.Ceiling(maxBottom) + 1));
            }
            catch
            {
                return (1, 1);
            }
        }

        private static double MeasureTextWidth(string text, string fontFamily, double fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var tf = new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var ft = new FormattedText(text,
                                       CultureInfo.CurrentUICulture,
                                       FlowDirection.LeftToRight,
                                       tf,
                                       fontSize,
                                       Brushes.Black,
                                       1.0);
            return ft.WidthIncludingTrailingWhitespace;
        }

        private static string? TryGetString(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : null;

        private static double TryGetDouble(JsonElement el, string prop, double def = 0)
        {
            if (!el.TryGetProperty(prop, out var v)) return def;
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetDouble(out var d) ? d : def,
                JsonValueKind.String => double.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d2) ? d2 : def,
                _ => def
            };
        }

        private static void TrySet(object obj, string name, object? value)
        {
            var pi = obj.GetType().GetProperty(name);
            if (pi == null || !pi.CanWrite) return;
            try { pi.SetValue(obj, value); } catch { /* ignore */ }
        }

        // ----------------------- CaseData helpery -----------------------

        private DomainCaseDataSnapshot BuildSnapshotFromVm(Guid caseId, Guid formVersionId)
        {
            var snap = new DomainCaseDataSnapshot
            {
                CaseId = caseId,
                FormVersionId = formVersionId,
                Values = new Dictionary<string, string>(),
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var block in _vm.Blocks)
            {
                foreach (var comp in block.Components)
                {
                    var fk = TryGetPropertyString(comp, "FieldKey");
                    if (string.IsNullOrWhiteSpace(fk)) continue;

                    var val = ReadValueAsString(comp);
                    if (val != null)
                        snap.Values[fk!] = val;
                }
            }

            return snap;
        }

        private void ApplyValuesToVm(Dictionary<string, string> values)
        {
            foreach (var block in _vm.Blocks)
            {
                foreach (var comp in block.Components)
                {
                    var fk = TryGetPropertyString(comp, "FieldKey");
                    if (string.IsNullOrWhiteSpace(fk)) continue;

                    if (values.TryGetValue(fk!, out var v))
                        TrySetValue(comp, v);
                }
            }
        }

        private static string? ReadValueAsString(object comp)
        {
            var pi = comp.GetType().GetProperty("Value");
            if (pi == null) return null;

            var v = pi.GetValue(comp);

            if (v == null) return null;
            if (v is string s) return s;

            if (v is DateTime dt)
                return dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("o", CultureInfo.InvariantCulture);

            if (v is bool b) return b ? "true" : "false";
            if (v is IFormattable fmt) return fmt.ToString(null, CultureInfo.InvariantCulture);

            return v.ToString();
        }

        private static void TrySetValue(object comp, string? raw)
        {
            var pi = comp.GetType().GetProperty("Value");
            if (pi == null || !pi.CanWrite) return;

            var t = pi.PropertyType;

            try
            {
                if (t == typeof(string))
                {
                    pi.SetValue(comp, raw);
                }
                else if (t == typeof(bool) || t == typeof(bool?))
                {
                    if (bool.TryParse(raw, out var b)) pi.SetValue(comp, b);
                }
                else if (t == typeof(DateTime) || t == typeof(DateTime?))
                {
                    if (DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var d) ||
                        DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out d))
                    {
                        pi.SetValue(comp, d);
                    }
                }
                else if (t == typeof(int) || t == typeof(int?))
                {
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) pi.SetValue(comp, i);
                }
                else if (t == typeof(decimal) || t == typeof(decimal?))
                {
                    if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec)) pi.SetValue(comp, dec);
                }
                else if (t == typeof(double) || t == typeof(double?))
                {
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) pi.SetValue(comp, d);
                }
                else
                {
                    var cast = Convert.ChangeType(raw, Nullable.GetUnderlyingType(t) ?? t, CultureInfo.InvariantCulture);
                    pi.SetValue(comp, cast);
                }
            }
            catch
            {
                // nebrzdit UI – problémovou hodnotu prostě nepřepíšeme
            }
        }

        private static string? TryGetPropertyString(object obj, string name)
        {
            var pi = obj.GetType().GetProperty(name);
            if (pi == null) return null;
            try { var v = pi.GetValue(obj, null); return v?.ToString(); }
            catch { return null; }
        }

        // ----------------------- Schema storage helpers -----------------------

        private static string? LoadBlockSchemaJson(Guid blockId, string? version)
        {
            if (blockId == Guid.Empty) return null;
            var dir = Agt.Infrastructure.JsonStore.JsonPaths.Dir("blocks");
            Directory.CreateDirectory(dir);
            var ver = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version!.Trim();
            var path = System.IO.Path.Combine(dir, $"{blockId:D}__{ver}.json");
            if (!File.Exists(path)) return null;
            try { return File.ReadAllText(path); } catch { return null; }
        }

        // ----------------------- Advance -----------------------
        private async Task TryAdvanceCaseAsync(Guid caseId, Guid stageId)
        {
            try
            {
                var caseSvc = _sp.GetService<Agt.Domain.Abstractions.ICaseService>();
                var caseRepo = _sp.GetService<ICaseRepository>();

                if (caseSvc == null || caseRepo == null)
                {
                    SafeLog("Advance: missing services (ICaseService/ICaseRepository). Skipping advance.");
                    return;
                }

                var all = caseRepo.ListBlocks(caseId).ToList();
                var openInStage = all
                    .Where(b => b.State == CaseBlockState.Open)
                    .Where(b => _layout.Blocks.FirstOrDefault(bl => bl.BlockId == b.BlockDefinitionId)?.StageId == stageId)
                    .ToList();

                SafeLog($"Advance: open blocks in this stage = {openInStage.Count}");

                var actor = Guid.Parse("00000000-0000-0000-0000-000000000001");
                foreach (var b in openInStage)
                {
                    try
                    {
                        caseSvc.CompleteBlock(b.Id, actor);
                        SafeLog($"Advance: Completed block instance={b.Id} (def={b.BlockDefinitionId})");
                    }
                    catch (Exception ex)
                    {
                        SafeLog($"Advance: CompleteBlock FAILED for instance={b.Id}: {ex.Message}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                SafeLog("Advance ERROR: " + ex);
            }
        }

        // ----------------------- VM + DTOs -----------------------

        private sealed class RunStageVm
        {
            public ObservableCollection<RunBlockVm> Blocks { get; } = new();
        }

        private sealed class RunBlockVm
        {
            public Guid BlockId { get; set; }
            public string InstanceKey { get; set; } = "";
            public string Title { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public double PreviewWidth { get; set; } = 320;
            public double PreviewHeight { get; set; } = 200;
            public RenderMode Mode { get; set; } = RenderMode.Run;
            public ObservableCollection<FieldComponentBase> Components { get; } = new();
        }

        private sealed class LayoutSnapshot
        {
            public string? SourcePath { get; set; }
            public List<StageLayout> Stages { get; set; } = new();
            public List<BlockLayout> Blocks { get; set; } = new();
        }

        private sealed class StageLayout
        {
            public Guid Id { get; set; }
            public string? Title { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        private sealed class BlockLayout
        {
            public Guid BlockId { get; set; } = Guid.Empty;
            public string Version { get; set; } = "1.0.0";
            public string? Title { get; set; }
            public Guid StageId { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string? LegacyInstanceKey { get; set; }
            public string? LegacyDefKey { get; set; }
            public string? SchemaJson { get; set; }
        }
    }

    public static class StageRunUiBus
    {
        public static event EventHandler<Guid>? CaseChanged;
        public static void PublishCaseChanged(Guid caseId)
        {
            try { CaseChanged?.Invoke(null, caseId); } catch { }
        }
    }
}
