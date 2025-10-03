/*using Agt.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace Agt.Desktop.ViewModels
{

    public sealed class FormProcessEditorViewModel : ViewModelBase
    {
        public GraphVm Graph { get; } = new();
        public ObservableCollection<PaletteItem> Palette { get; } = new();

        // Dostupné entity (připraveno na AD: a) dnes in-memory, b) později napojíme na provider)
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
            // Dostupní uživatelé/skupiny (DEMO) — později nahradíme AD providerem
            SeedDirectoryDemo();
        }
        /// <summary>
        /// Načte položky palety z knihovny bloků (JSON na disku).
        /// Volat po vytvoření VM nebo kdykoli při „Obnovit paletu“.
        /// </summary>
        public void LoadPaletteFromLibrary(IBlockLibrary lib)
        {
            if (lib == null) return;

            var items = lib.Enumerate().ToList();
            Palette.Clear();
            foreach (var it in items)
                Palette.Add(new PaletteItem(it.Key, it.Name, it.Version));
        }
        private void SeedDirectoryDemo()
        {
            // Users (demo)
            var users = new[]
            {
                "jan.novak", "petr.svoboda", "karel.dvorak", "eva.prochazkova",
                "lucie.kralova", "martin.horak", "hana.mala", "ondrej.cerny",
                "tomas.pospisil", "iva.kucerova"
            };
            foreach (var u in users) AvailableUsers.Add(u);

            // Groups (demo)
            var groups = new[]
            {
                "QC-Operators", "QC-Leads", "Production-ShiftA", "Production-ShiftB",
                "Engineering-Design", "Engineering-Process", "Logistics", "Warehouse"
            };
            foreach (var g in groups) AvailableGroups.Add(g);
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

        // jemný snap
        public double Snap(double v, double grid, bool noSnap)
        {
            if (noSnap || grid <= 0) return v;
            return Math.Round(v / grid) * grid;
        }

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
            SelectedBlock = null;
            SelectedStageEdge = null;
            SelectedStage = null;
        }

        public void SelectStage(StageVm? s)
        {
            foreach (var st in Graph.Stages) st.IsSelected = false;
            if (s != null) s.IsSelected = true;
            SelectedStage = s;
            SelectedBlock = null;
            SelectedStageEdge = null;
            Raise(nameof(Graph));
        }

        public void SelectBlock(BlockVm? b)
        {
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            if (b != null) b.IsSelected = true;
            SelectedBlock = b;
            SelectedStage = null;
            SelectedStageEdge = null;
            Raise(nameof(Graph));
        }

        public void SelectEdge(StageEdgeVm? e)
        {
            SelectedStageEdge = e;
            SelectedBlock = null;
            SelectedStage = null;
            foreach (var st in Graph.Stages) foreach (var bb in st.Blocks) bb.IsSelected = false;
            foreach (var st in Graph.Stages) st.IsSelected = false;
            Raise(nameof(SelectedStageEdge));
        }

        public void AddStageEdge(StageVm from, StageVm to)
            => Graph.StageEdges.Add(new StageEdgeVm { Id = Guid.NewGuid(), FromStageId = from.Id, ToStageId = to.Id, ConditionJson = @"{ ""conditions"": [] }" });

        // clampování bloků do stage
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

        // --- Náhled bloku: jednoduché typy polí ---
        public void GeneratePreview(BlockVm b)
        {
            b.PreviewFields.Clear();

            if (b.Key.Equals("QC_Input", StringComparison.OrdinalIgnoreCase))
            {
                b.PreviewFields.Add(new PreviewFieldVm("Číslo dílu", "Text"));
                b.PreviewFields.Add(new PreviewFieldVm("Počet ks", "Number"));
                b.PreviewFields.Add(new PreviewFieldVm("Operátor", "Select", "Novák;Svoboda;Dvořák"));
            }
            else if (b.Key.Equals("VisualCheck", StringComparison.OrdinalIgnoreCase))
            {
                b.PreviewFields.Add(new PreviewFieldVm("Scratch / Dent", "Checkbox"));
                b.PreviewFields.Add(new PreviewFieldVm("Color mismatch", "Checkbox"));
                b.PreviewFields.Add(new PreviewFieldVm("Datum kontroly", "Date"));
            }
            else if (b.Key.Equals("Neshoda", StringComparison.OrdinalIgnoreCase))
            {
                b.PreviewFields.Add(new PreviewFieldVm("Typ neshody", "Select", "Rozměr;Vzhled;Funkce"));
                b.PreviewFields.Add(new PreviewFieldVm("Závažnost", "Select", "Nízká;Střední;Vysoká"));
                b.PreviewFields.Add(new PreviewFieldVm("Popis", "Text"));
            }
            else
            {
                b.PreviewFields.Add(new PreviewFieldVm("Pole 1", "Text"));
                b.PreviewFields.Add(new PreviewFieldVm("Pole 2", "Text"));
            }
        }

        // --- Uložení rozpracovaného návrhu (volitelné) ---
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
                object? cond = null;
                try { cond = JsonSerializer.Deserialize<object>(ed.ConditionJson); }
                catch { cond = new { }; }
                return new { fromStage = from.Title, toStage = to.Title, condition = cond };
            }).ToList();

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "editor_pins.json"),
                JsonSerializer.Serialize(pins, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(dir, "editor_stage_routes.json"),
                JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
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

        // přiřazení
        public ObservableCollection<string> AssignedGroups { get; } = new();
        public ObservableCollection<string> AssignedUsers { get; } = new();

        public string AssignedGroupsCsv
        {
            get => string.Join(", ", AssignedGroups);
            set
            {
                AssignedGroups.Clear();
                foreach (var part in SplitCsv(value)) AssignedGroups.Add(part);
                Raise();
            }
        }
        public string AssignedUsersCsv
        {
            get => string.Join(", ", AssignedUsers);
            set
            {
                AssignedUsers.Clear();
                foreach (var part in SplitCsv(value)) AssignedUsers.Add(part);
                Raise();
            }
        }

        // nastavení běhu
        private string _startMode = "Manual"; // Manual | AutoOnPrevComplete | AutoOnCondition
        public string StartMode { get => _startMode; set { _startMode = value; Raise(); } }

        private int _slaHours = 0; // 0 = bez SLA
        public int SLAHours { get => _slaHours; set { _slaHours = value; Raise(); } }

        private bool _allowReopen = true;
        public bool AllowReopen { get => _allowReopen; set { _allowReopen = value; Raise(); } }

        private bool _autoCompleteOnAllBlocks = false;
        public bool AutoCompleteOnAllBlocks { get => _autoCompleteOnAllBlocks; set { _autoCompleteOnAllBlocks = value; Raise(); } }

        public ObservableCollection<BlockVm> Blocks { get; } = new();

        private static string[] SplitCsv(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();
            return csv.Split(',', ';')
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToArray();
        }
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

        public ObservableCollection<PreviewFieldVm> PreviewFields { get; } = new();
    }

    public sealed class PreviewFieldVm : ViewModelBase
    {
        public string Label { get; }
        public string FieldType { get; }
        public string? Options { get; }

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
*/