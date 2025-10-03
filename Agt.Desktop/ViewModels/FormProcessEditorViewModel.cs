using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels
{





    public sealed class FormProcessEditorViewModel : ViewModelBase
    {
        public GraphVm Graph { get; } = new();
        public ObservableCollection<PaletteItem> Palette { get; } = new();

        public ObservableCollection<string> AvailableUsers { get; } = new();
        public ObservableCollection<string> AvailableGroups { get; } = new();

        private StageVm? _selectedStage;
        public StageVm? SelectedStage { get => _selectedStage; set { _selectedStage = value; Raise(); } }

        private BlockVm? _selectedBlock;
        public BlockVm? SelectedBlock { get => _selectedBlock; set { _selectedBlock = value; Raise(); } }

        private StageEdgeVm? _selectedStageEdge;
        public StageEdgeVm? SelectedStageEdge { get => _selectedStageEdge; set { _selectedStageEdge = value; Raise(); } }

        public FormProcessEditorViewModel()
        {
            SeedDirectoryDemo();
        }

        public void LoadPaletteFromLibrary(IBlockLibrary lib)
        {
            if (lib == null) return;
            Palette.Clear();
            foreach (var it in lib.Enumerate())
                Palette.Add(new PaletteItem(it.Key, it.Name, it.Version));
        }

        private void SeedDirectoryDemo()
        {
            foreach (var u in new[]
            {
                "jan.novak","petr.svoboda","karel.dvorak","eva.prochazkova",
                "lucie.kralova","martin.horak","hana.mala","ondrej.cerny",
                "tomas.pospisil","iva.kucerova"
            }) AvailableUsers.Add(u);

            foreach (var g in new[]
            {
                "QC-Operators","QC-Leads","Production-ShiftA","Production-ShiftB",
                "Engineering-Design","Engineering-Process","Logistics","Warehouse"
            }) AvailableGroups.Add(g);
        }

        // --- API ---
        public void AddStage(string title, double x, double y, double w, double h)
            => Graph.Stages.Add(new StageVm { Id = Guid.NewGuid(), Title = title, X = x, Y = y, W = w, H = h });

        public BlockVm AddBlock(StageVm stage, string key, string title, string version, double x, double y)
        {
            var b = new BlockVm { Id = Guid.NewGuid(), Key = key, Title = title, Version = version, X = x, Y = y };
            stage.Blocks.Add(b);
            return b;
        }

        public StageVm? FindStage(Guid id) => Graph.Stages.FirstOrDefault(s => s.Id == id);

        public StageVm? HitTestStage(Point p, double tolerance)
        {
            foreach (var st in Graph.Stages)
            {
                var body = new Rect(st.X, st.Y, st.W, st.H);
                body.Inflate(tolerance, tolerance);
                if (body.Contains(p)) return st;
            }
            return null;
        }

        // snap
        public double Snap(double v, double grid, bool noSnap)
            => (noSnap || grid <= 0) ? v : Math.Round(v / grid) * grid;

        public void SnapAll(double grid)
        {
            foreach (var st in Graph.Stages)
            {
                st.X = Snap(st.X, grid, false); st.Y = Snap(st.Y, grid, false);
                st.W = Math.Max(200, Snap(st.W, grid, false)); st.H = Math.Max(150, Snap(st.H, grid, false));
                foreach (var b in st.Blocks)
                {
                    b.X = Snap(b.X, grid, false); b.Y = Snap(b.Y, grid, false);
                }
            }
        }

        public (double X, double Y) GetNextBlockPosition(StageVm st, double baseX, double baseY)
        {
            var x = baseX; var y = baseY;
            while (st.Blocks.Any(b => Math.Abs(b.X - x) < 10 && Math.Abs(b.Y - y) < 10))
            {
                x += 30; y += 20;
                if (x + 260 > st.W) { x = baseX; y += 30; }
                if (y + 140 > st.H) { x = baseY + 40; y = baseY + 40; break; }
            }
            return (Snap(x, 10, false), Snap(y, 10, false));
        }

        // výběry
        public void ClearSelection()
        {
            foreach (var s in Graph.Stages) s.IsSelected = false;
            foreach (var s in Graph.Stages) foreach (var b in s.Blocks) b.IsSelected = false;
            SelectedBlock = null; SelectedStageEdge = null; SelectedStage = null;
        }

        public void SelectStage(StageVm? s)
        {
            foreach (var st in Graph.Stages) st.IsSelected = false;
            if (s != null) s.IsSelected = true;
            SelectedStage = s; SelectedBlock = null; SelectedStageEdge = null; Raise(nameof(Graph));
        }

        public void SelectBlock(BlockVm? b)
        {
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            if (b != null) b.IsSelected = true;
            SelectedBlock = b; SelectedStage = null; SelectedStageEdge = null; Raise(nameof(Graph));
        }

        public void SelectEdge(StageEdgeVm? e)
        {
            SelectedStageEdge = e;
            SelectedBlock = null; SelectedStage = null;
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            foreach (var st in Graph.Stages) st.IsSelected = false;
            Raise(nameof(SelectedStageEdge));
        }

        public void AddStageEdge(StageVm from, StageVm to)
            => Graph.StageEdges.Add(new StageEdgeVm { Id = Guid.NewGuid(), FromStageId = from.Id, ToStageId = to.Id, ConditionJson = @"{ ""conditions"": [] }" });

        public void ClampBlockInside(BlockVm b, StageVm st, double blockW, double blockH, double header)
        {
            b.X = Math.Max(0, Math.Min(b.X, st.W - blockW));
            b.Y = Math.Max(0, Math.Min(b.Y, st.H - header - blockH));
        }

        public void ClampBlocksInside(StageVm st, double blockW, double blockH, double header)
        {
            foreach (var b in st.Blocks)
                ClampBlockInside(b, st, blockW, blockH, header);
        }

        // --- Porty stage (absolutní souřadnice) ---
        private const double _portOffset = 12.0;
        public (double X, double Y) GetStageOutPortAbs(StageVm s) => (s.X + s.W - _portOffset, s.Y + s.H / 2.0);
        public (double X, double Y) GetStageInPortAbs(StageVm s) => (s.X + _portOffset, s.Y + s.H / 2.0);

        // ===================== PREVIEW Z JSON KNIHOVNY (robustní, rekurzivní) =====================

        public void GeneratePreview(BlockVm b)
        {
            b.PreviewElements.Clear();

            if (!BlockLibraryJson.Default.TryLoadByKeyVersion(b.Key, b.Version, out var doc, out _))
            {
                // fallback
                b.PreviewWidth = 320; b.PreviewHeight = 200;
                b.PreviewElements.Add(new PreviewElementVm("Pole 1", "Text", null, 8, 8, 220, 28));
                b.PreviewElements.Add(new PreviewElementVm("Pole 2", "Text", null, 8, 44, 220, 28));
                return;
            }

            try
            {
                var root = doc!.RootElement;

                // 1) Rozměr plátna
                var size = TryExtractCanvasSize(root);
                b.PreviewWidth = size.W > 80 ? size.W : 320;
                b.PreviewHeight = size.H > 60 ? size.H : 200;

                // 2) Extract fields (s umístěním)
                var fields = ExtractPreviewElements(root).ToList();

                if (fields.Count == 0)
                {
                    // fallback
                    b.PreviewElements.Add(new PreviewElementVm("Pole 1", "Text", null, 8, 8, 220, 28));
                    b.PreviewElements.Add(new PreviewElementVm("Pole 2", "Text", null, 8, 44, 220, 28));
                }
                else
                {
                    foreach (var f in fields) b.PreviewElements.Add(f);

                    // Pokud nenajdeme size, dopočti z bounding boxu
                    if (size.W <= 80 || size.H <= 60)
                    {
                        var maxX = fields.Max(e => e.X + e.W);
                        var maxY = fields.Max(e => e.Y + e.H);
                        b.PreviewWidth = Math.Max(b.PreviewWidth, maxX + 16);
                        b.PreviewHeight = Math.Max(b.PreviewHeight, maxY + 16);
                    }
                }
            }
            catch
            {
                // fallback při chybě JSONu
                b.PreviewWidth = 320; b.PreviewHeight = 200;
                b.PreviewElements.Add(new PreviewElementVm("Pole 1", "Text", null, 8, 8, 220, 28));
                b.PreviewElements.Add(new PreviewElementVm("Pole 2", "Text", null, 8, 44, 220, 28));
            }
            finally
            {
                doc?.Dispose();
            }
        }

        // ===== helpers pro preview z JSONu =====
        private static (double W, double H) TryExtractCanvasSize(JsonElement root)
        {
            // Canvas.Width/Height
            if (TryObject(root, "Canvas") is JsonElement canvas)
            {
                var w = GetDouble(canvas, "Width") ?? GetDouble(canvas, "W");
                var h = GetDouble(canvas, "Height") ?? GetDouble(canvas, "H");
                if (w.HasValue && h.HasValue) return (w.Value, h.Value);
            }

            // Block.Size { W,H } / Definition.Size
            foreach (var parentName in new[] { "Block", "Definition" })
            {
                if (TryObject(root, parentName) is JsonElement parent)
                {
                    if (TryObject(parent, "Size") is JsonElement size)
                    {
                        var w = GetDouble(size, "Width") ?? GetDouble(size, "W");
                        var h = GetDouble(size, "Height") ?? GetDouble(size, "H");
                        if (w.HasValue && h.HasValue) return (w.Value, h.Value);
                    }
                }
            }

            return (0, 0);
        }
        private static bool TryPathArray(JsonElement root, string[] path, out JsonElement? arr)
        {
            arr = null;
            var cur = root;
            foreach (var seg in path)
            {
                if (!TryProp(cur, seg, out var v)) return false;
                cur = v;
            }
            if (cur.ValueKind == JsonValueKind.Array) { arr = cur; return true; }
            return false;
        }

        private static IEnumerable<PreviewElementVm> ExtractPreviewElements(JsonElement root)
        {
            // kandidáti kolekcí
            var arrays = new List<JsonElement>();
            foreach (var path in new[]
            {
        new[] { "Fields" },
        new[] { "Schema", "Fields" },
        new[] { "Block", "Fields" },
        new[] { "Block", "Components" },
        new[] { "Definition", "Fields" },
        new[] { "Components" }
    })
            {
                if (TryPathArray(root, path, out var arr)) arrays.Add(arr.Value);
            }

            foreach (var arr in arrays)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var label = GetString(el, "Label") ?? GetString(el, "Name") ?? GetString(el, "Title") ?? GetString(el, "Id");
                    if (string.IsNullOrWhiteSpace(label)) continue;

                    var type = NormalizeType(GetString(el, "FieldType") ?? GetString(el, "Type") ?? GetString(el, "ControlType") ?? "Text");
                    var options = GetOptions(el, "Options") ?? GetOptions(el, "Items") ?? GetOptions(el, "Values");

                    // pozice a rozměry (tolerantní názvy)
                    var x = GetDouble(el, "X") ?? GetDouble(el, "Left") ?? 8;
                    var y = GetDouble(el, "Y") ?? GetDouble(el, "Top") ?? 8;
                    var w = GetDouble(el, "W") ?? GetDouble(el, "Width") ?? GuessWidth(type);
                    var h = GetDouble(el, "H") ?? GetDouble(el, "Height") ?? GuessHeight(type);

                    yield return new PreviewElementVm(label!, type, options, x, y, w, h);
                }
            }
        }
        private static JsonElement? TryObject(JsonElement el, string propName)
        {
            if (TryProp(el, propName, out var v) && v.ValueKind == JsonValueKind.Object)
                return v;
            return null;
        }

        private static double GuessWidth(string type) =>
            type == "Checkbox" ? 18 : 220;
        private static double GuessHeight(string type) =>
            type switch { "Date" => 28, "Select" => 28, "Number" => 28, "Text" => 28, "Checkbox" => 18, _ => 28 };

        private static double? GetDouble(JsonElement el, string propName)
        {
            if (!TryProp(el, propName, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetDouble(out var d) ? d : (double?)null,
                JsonValueKind.String => double.TryParse(v.GetString(), out var d2) ? d2 : (double?)null,
                _ => null
            };
        }

        private static void AddFallbackFields(BlockVm b)
        {

        }

        /// <summary>
        /// Projde JSON do hloubky a hledá objekty, které vypadají jako field:
        /// - mají některé z: Label/Name/Title/Placeholder
        /// - a/nebo mají typ: FieldType/Type/ControlType/Kind/DataType
        /// - volby: Options/Items/Values/Enum/Choices
        /// </summary>
        private static IEnumerable<PreviewFieldVm> CollectFieldCandidates(JsonElement root)
        {
            foreach (var obj in EnumerateObjectsDeep(root))
            {
                var label = GetString(obj, "Label")
                            ?? GetString(obj, "Name")
                            ?? GetString(obj, "Title")
                            ?? GetString(obj, "Placeholder");

                // typ
                var type = GetString(obj, "FieldType")
                           ?? GetString(obj, "Type")
                           ?? GetString(obj, "ControlType")
                           ?? GetString(obj, "Kind")
                           ?? GetString(obj, "DataType");

                // pokud nemá ani label ani typ, přeskoč
                if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(type))
                    continue;

                // normalizuj typ (i z názvů WPF prvků)
                var norm = NormalizeType(type);

                // options
                var options = GetOptions(obj, "Options")
                              ?? GetOptions(obj, "Items")
                              ?? GetOptions(obj, "Values")
                              ?? GetOptions(obj, "Enum")
                              ?? GetOptions(obj, "Choices");

                // Pokud není label, ale víme typ, udělej generický
                if (string.IsNullOrWhiteSpace(label)) label = $"({norm})";

                // Heuristika: ignoruj zjevně nekonfigurační objekty (např. XY souřadnice apod.)
                // Když není nic rozumného, přeskoč.
                if (string.IsNullOrWhiteSpace(label)) continue;

                yield return new PreviewFieldVm(label, norm, options);
            }
        }

        private static IEnumerable<JsonElement> EnumerateObjectsDeep(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    yield return el;
                    foreach (var p in el.EnumerateObject())
                        foreach (var sub in EnumerateObjectsDeep(p.Value)) yield return sub;
                    break;

                case JsonValueKind.Array:
                    foreach (var it in el.EnumerateArray())
                        foreach (var sub in EnumerateObjectsDeep(it)) yield return sub;
                    break;
            }
        }

        private static string NormalizeType(string? t)
        {
            if (string.IsNullOrWhiteSpace(t)) return "Text";
            var s = t.Trim().ToLowerInvariant();

            // obecné
            if (s is "string" or "text" or "textbox" or "input" or "textarea") return "Text";
            if (s is "int" or "integer" or "number" or "numeric" or "double" or "decimal") return "Number";
            if (s is "date" or "datetime" or "datetime-local" or "datepicker") return "Date";
            if (s is "select" or "dropdown" or "combo" or "combobox" or "options" or "list") return "Select";
            if (s is "bool" or "boolean" or "checkbox" or "check") return "Checkbox";

            // názvy WPF komponent
            if (s.Contains("combobox")) return "Select";
            if (s.Contains("checkbox")) return "Checkbox";
            if (s.Contains("textbox")) return "Text";
            if (s.Contains("numeric")) return "Number";
            if (s.Contains("date")) return "Date";

            return "Text";
        }

        private static string? GetOptions(JsonElement el, string propName)
        {
            if (!TryProp(el, propName, out var p)) return null;

            switch (p.ValueKind)
            {
                case JsonValueKind.Array:
                    var arr = p.EnumerateArray()
                               .Select(i => i.ValueKind == JsonValueKind.String ? i.GetString() : i.ToString())
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .ToArray();
                    return arr.Length == 0 ? null : string.Join(';', arr);

                case JsonValueKind.String:
                    return p.GetString();

                case JsonValueKind.Object:
                    // třeba dictionary { "A":"Text A","B":"Text B" } → vezmeme klíče
                    var keys = new List<string>();
                    foreach (var kv in p.EnumerateObject())
                        keys.Add(kv.Name);
                    return keys.Count == 0 ? null : string.Join(';', keys);

                default:
                    return null;
            }
        }

        private static bool TryProp(JsonElement el, string propName, out JsonElement val)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(propName) ||
                    string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    val = p.Value; return true;
                }
            }
            val = default;
            return false;
        }

        private static string? GetString(JsonElement el, string propName)
        {
            if (!TryProp(el, propName, out var v)) return null;
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            return v.ToString();
        }

        // --- Draft export (volitelné) ---
        public void SaveDraftToJsonFiles()
        {
            var pins = Graph.Stages.SelectMany(s => s.Blocks)
                .Select(b => new { key = b.Key, version = b.Version })
                .Distinct()
                .ToList();

            var routes = Graph.StageEdges.Select(ed =>
            {
                var from = FindStage(ed.FromStageId)!;
                var to = FindStage(ed.ToStageId)!;
                object? cond;
                try { cond = JsonSerializer.Deserialize<object>(ed.ConditionJson); }
                catch { cond = new { }; }
                return new { fromStage = from.Title, toStage = to.Title, condition = cond };
            }).ToList();

            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = System.IO.Path.Combine(dir, "AGT");
            System.IO.Directory.CreateDirectory(path);
            System.IO.File.WriteAllText(System.IO.Path.Combine(path, "editor_pins.json"),
                System.Text.Json.JsonSerializer.Serialize(pins, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.WriteAllText(System.IO.Path.Combine(path, "editor_stage_routes.json"),
                System.Text.Json.JsonSerializer.Serialize(routes, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public sealed class GraphVm : ViewModelBase
    {
        public ObservableCollection<StageVm> Stages { get; } = new();
        public ObservableCollection<StageEdgeVm> StageEdges { get; } = new();
    }

    public sealed class StageVm : ViewModelBase
    {
        public Guid Id { get; set; }

        private string _title = "Stage";
        public string Title { get => _title; set { _title = value; Raise(); } }

        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }
        private double _w = 600; public double W { get => _w; set { _w = Math.Max(200, value); Raise(); } }
        private double _h = 400; public double H { get => _h; set { _h = Math.Max(150, value); Raise(); } }

        private bool _sel; public bool IsSelected { get => _sel; set { _sel = value; Raise(); } }

        public ObservableCollection<string> AssignedGroups { get; } = new();
        public ObservableCollection<string> AssignedUsers { get; } = new();

        private string _startMode = "Manual";
        public string StartMode { get => _startMode; set { _startMode = value; Raise(); } }

        private int _slaHours = 0; public int SLAHours { get => _slaHours; set { _slaHours = value; Raise(); } }
        private bool _allowReopen = true; public bool AllowReopen { get => _allowReopen; set { _allowReopen = value; Raise(); } }
        private bool _autoCompleteOnAllBlocks = false; public bool AutoCompleteOnAllBlocks { get => _autoCompleteOnAllBlocks; set { _autoCompleteOnAllBlocks = value; Raise(); } }

        public ObservableCollection<BlockVm> Blocks { get; } = new();
    }

    public sealed class BlockVm : ViewModelBase
    {
        public Guid Id { get; set; }
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public string Version { get; set; } = "1.0.0";

        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }

        private bool _sel; public bool IsSelected { get => _sel; set { _sel = value; Raise(); } }

        // NOVÉ: vykreslované prvky a rozměry plátna
        public ObservableCollection<PreviewElementVm> PreviewElements { get; } = new();

        private double _previewWidth = 320;
        public double PreviewWidth { get => _previewWidth; set { _previewWidth = value; Raise(); } }

        private double _previewHeight = 200;
        public double PreviewHeight { get => _previewHeight; set { _previewHeight = value; Raise(); } }
    }

    public sealed class PreviewElementVm : ViewModelBase
    {
        public string Label { get; }
        public string FieldType { get; }   // Text/Number/Date/Select/Checkbox
        public string? Options { get; }    // pro Select: "A;B;C"

        public double X { get; }
        public double Y { get; }
        public double W { get; }
        public double H { get; }

        public PreviewElementVm(string label, string fieldType, string? options,
                                double x, double y, double w, double h)
        {
            Label = label;
            FieldType = fieldType;
            Options = options;
            X = x; Y = y; W = w; H = h;
        }
    }

    public sealed class PreviewFieldVm : ViewModelBase
    {
        public string Label { get; }
        public string FieldType { get; } // Text / Number / Date / Select / Checkbox
        public string? Options { get; }   // pro Select (např. "A;B;C")

        public PreviewFieldVm(string label, string fieldType, string? options = null)
        {
            Label = label;
            FieldType = fieldType;
            Options = options;
        }
    }

    public sealed class StageEdgeVm : ViewModelBase
    {
        public Guid Id { get; set; }
        public Guid FromStageId { get; set; }
        public Guid ToStageId { get; set; }
        public string ConditionJson { get; set; } = @"{ ""conditions"": [] }";
    }

    public sealed class PaletteItem
    {
        public string Key { get; }
        public string Name { get; }
        public string Version { get; }
        public string Display => $"{Name}   [{Key}]   v{Version}";

        public PaletteItem(string key, string name, string version)
        {
            Key = key; Name = name; Version = version;
        }
    }
}
