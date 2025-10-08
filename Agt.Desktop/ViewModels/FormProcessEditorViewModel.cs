using Agt.Desktop.Models;
using Agt.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

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
            // Paleta se načítá z knihovny ve window code-behind
            SeedDirectoryDemo();
        }

        // ====== Knihovna bloků (paleta) ======
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
            foreach (var u in new[]
            {
                "jan.novak", "petr.svoboda", "karel.dvorak", "eva.prochazkova",
                "lucie.kralova", "martin.horak", "hana.mala", "ondrej.cerny",
                "tomas.pospisil", "iva.kucerova"
            }) AvailableUsers.Add(u);

            foreach (var g in new[]
            {
                "QC-Operators", "QC-Leads", "Production-ShiftA", "Production-ShiftB",
                "Engineering-Design", "Engineering-Process", "Logistics", "Warehouse"
            }) AvailableGroups.Add(g);
        }

        // ====== API pro editor ======
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

        public void ClampBlocksInside(StageVm st, double blockW, double blockH, double header)
        {
            foreach (var b in st.Blocks)
                ClampBlockInside(b, st, blockW, blockH, header);
        }

        // ====== Porty stage (absolutní souřadnice) ======
        private const double _portOffset = 12.0;
        public (double X, double Y) GetStageOutPortAbs(StageVm s) => (s.X + s.W - _portOffset, s.Y + s.H / 2.0);
        public (double X, double Y) GetStageInPortAbs(StageVm s) => (s.X + _portOffset, s.Y + s.H / 2.0);

        // ===================== PREVIEW Z JSON KNIHOVNY =====================
        /// <summary>
        /// Naplní náhled bloku (rozměry + prvky) podle reálného JSONu v knihovně (Key + Version).
        /// </summary>
        public void GeneratePreview(BlockVm b)
        {
            b.Components.Clear();

            if (!BlockLibraryJson.Default.TryLoadByKeyVersion(b.Key, b.Version, out var doc, out _))
            {
                b.PreviewWidth = Math.Max(b.PreviewWidth, 1);
                b.PreviewHeight = Math.Max(b.PreviewHeight, 1);
                return;
            }

            try
            {
                var root = doc!.RootElement;

                var blockName = TryGetString(root, "BlockName");
                if (!string.IsNullOrWhiteSpace(blockName) && string.IsNullOrWhiteSpace(b.Title))
                    b.Title = blockName!;

                if (!TryProp(root, "Items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
                {
                    b.PreviewWidth = Math.Max(b.PreviewWidth, 1);
                    b.PreviewHeight = Math.Max(b.PreviewHeight, 1);
                    return;
                }

                double maxRight = 0, maxBottom = 0;

                foreach (var it in itemsEl.EnumerateArray())
                {
                    var x = TryGetDouble(it, "X") ?? 0;
                    var y = TryGetDouble(it, "Y") ?? 0;
                    var w = TryGetDouble(it, "Width") ?? 120;
                    var h = TryGetDouble(it, "Height") ?? 28;
                    var z = (int)(TryGetDouble(it, "ZIndex") ?? 0);

                    var typeKey = (TryGetString(it, "TypeKey") ?? "").Trim().ToLowerInvariant();
                    var label = TryGetString(it, "Label") ?? "";

                    var bgHex = TryGetString(it, "Background");
                    var fgHex = TryGetString(it, "Foreground");
                    var fontFam = TryGetString(it, "FontFamily");
                    var fontSz = TryGetDouble(it, "FontSize");

                    // ✅ použij systémové barvy jako výchozí
                    Brush? bg = !string.IsNullOrWhiteSpace(bgHex) ? ToBrushSafe(bgHex) : SystemColors.ControlBrush;
                    Brush? fg = !string.IsNullOrWhiteSpace(fgHex) ? ToBrushSafe(fgHex) : SystemColors.ControlTextBrush;

                    var ff = !string.IsNullOrWhiteSpace(fontFam) ? fontFam : "Segoe UI";
                    var fs = fontSz ?? 12;

                    FieldComponentBase component = typeKey switch
                    {
                        "label" => new LabelField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "textbox" => new TextBoxField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "textarea" => new TextAreaField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "number" => new NumberField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "date" => new DateField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "combobox" => new ComboBoxField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        "checkbox" => new CheckBoxField { Label = label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs },
                        _ => new LabelField { Label = string.IsNullOrWhiteSpace(label) ? $"[{typeKey}]" : label, Width = w, Height = h, Background = bg, Foreground = fg, FontFamily = ff, FontSize = fs }
                    };

                    component.X = x;
                    component.Y = y;
                    component.ZIndex = z;

                    // výpočet rozměrů pro vykreslení – už BEZ labelu!
                    double totalW = w;
                    double totalH = h;

                    switch (typeKey)
                    {
                        case "textbox":
                        case "textarea":
                        case "number":
                        case "date":
                        case "combobox":
                            totalW = w + 2;   // border left/right
                            totalH = h + 2;   // border top/bottom
                            break;

                        case "checkbox":
                            double textW = MeasureTextWidth(label, ff, fs);
                            totalW = Math.Max(w, 20 + 4 + textW);
                            totalH = Math.Max(h, 20);
                            break;

                        default:
                            totalW = w;
                            totalH = h;
                            break;
                    }

                    component.TotalWidth = totalW;
                    component.TotalHeight = totalH;

                    b.Components.Add(component);

                    maxRight = Math.Max(maxRight, x + totalW);
                    maxBottom = Math.Max(maxBottom, y + totalH);
                }

                // výsledek – přidej 1px bezpečnostní rezervu
                b.PreviewWidth = Math.Max(1, Math.Ceiling(maxRight) + 1);
                b.PreviewHeight = Math.Max(1, Math.Ceiling(maxBottom) + 1);
            }
            catch
            {
                b.PreviewWidth = Math.Max(b.PreviewWidth, 1);
                b.PreviewHeight = Math.Max(b.PreviewHeight, 1);
            }
            finally
            {
                doc?.Dispose();
            }
        }


        // 🔹 Pomocná metoda pro odhad šířky textu (checkbox text)
        private static double MeasureTextWidth(string text, string fontFamily, double fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var typeface = new System.Windows.Media.Typeface(
                new System.Windows.Media.FontFamily(fontFamily),
                System.Windows.FontStyles.Normal,
                System.Windows.FontWeights.Normal,
                System.Windows.FontStretches.Normal);

#pragma warning disable CS0618
            var ft = new System.Windows.Media.FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                fontSize,
                System.Windows.Media.Brushes.Transparent,
                1.0);
#pragma warning restore CS0618

            return Math.Ceiling(ft.Width);
        }


        // === SNAP + POHYB BEZ PŘEKRYVŮ A UVNITŘ STAGE ===


        // === pomocné ===
        private static bool RectIntersects(double x, double y, double w, double h, BlockVm other)
        {
            return !(x + w <= other.X ||
                     other.X + other.PreviewWidth <= x ||
                     y + h <= other.Y ||
                     other.Y + other.PreviewHeight <= y);
        }

        private static double SnapTo(double v, double grid) => grid <= 0 ? v : Math.Round(v / grid) * grid;

        /// <summary>
        /// Vrátí nejbližší volnou pozici ke (targetX,targetY) pro blok o rozměrech blockW/H.
        /// Respektuje snap na grid, clamp do stage a vyhne se kolizím.
        /// </summary>
        public (double X, double Y) FindNearestFreeSlot(StageVm st, double targetX, double targetY,
                                                        double blockW, double blockH,
                                                        double grid, double headerHeight,
                                                        int maxIterations = 200)
        {
            double baseX = SnapTo(targetX, grid);
            double baseY = SnapTo(targetY, grid);

            // základní clamp
            const double innerPad = 4;
            baseX = Math.Max(innerPad, Math.Min(baseX, st.W - blockW - innerPad));
            baseY = Math.Max(headerHeight + innerPad, Math.Min(baseY, st.H - blockH - innerPad));

            bool IsFree(double x, double y)
            {
                foreach (var other in st.Blocks)
                    if (RectIntersects(x, y, blockW, blockH, other)) return false;
                return true;
            }
            if (IsFree(baseX, baseY)) return (baseX, baseY);

            var step = Math.Max(1, (int)(grid > 0 ? grid : 8));
            int radius = step;
            int iter = 0;

            while (iter < maxIterations && radius < 4000)
            {
                for (int dx = -radius; dx <= radius; dx += step)
                {
                    // horní
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY - radius, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(headerHeight + innerPad, Math.Min(y, st.H - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                    // dolní
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY + radius, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(headerHeight + innerPad, Math.Min(y, st.H - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                }
                for (int dy = -radius + step; dy <= radius - step; dy += step)
                {
                    // levá
                    {
                        double x = SnapTo(baseX - radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(headerHeight + innerPad, Math.Min(y, st.H - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                    // pravá
                    {
                        double x = SnapTo(baseX + radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(innerPad, Math.Min(x, st.W - blockW - innerPad));
                        y = Math.Max(headerHeight + innerPad, Math.Min(y, st.H - blockH - innerPad));
                        if (IsFree(x, y)) return (x, y);
                        if (++iter >= maxIterations) break;
                    }
                }
                radius += step;
            }

            return (baseX, baseY); // nouzově
        }


        public void ClampBlockInside(BlockVm b, StageVm st, double blockW, double blockH, double header)
        {
            if (b == null || st == null) return;
            const double innerPad = 4; // malý vnitřní okraj
            b.X = Math.Max(innerPad, Math.Min(b.X, st.W - blockW - innerPad));
            b.Y = Math.Max(header + innerPad, Math.Min(b.Y, st.H - blockH - innerPad));
        }


        private static bool Intersects(BlockVm a, BlockVm b) =>
            !(a.X + a.PreviewWidth <= b.X ||
              b.X + b.PreviewWidth <= a.X ||
              a.Y + a.PreviewHeight <= b.Y ||
              b.Y + b.PreviewHeight <= a.Y);

        private void ResolveBlockOverlaps(BlockVm moved, StageVm st, double grid, double headerHeight)
        {
            for (int iter = 0; iter < 20; iter++)
            {
                var hit = st.Blocks.FirstOrDefault(b => !ReferenceEquals(b, moved) && Intersects(moved, b));
                if (hit == null) break;

                double dxRight = (hit.X + hit.PreviewWidth) - moved.X;
                double dxLeft = (moved.X + moved.PreviewWidth) - hit.X;
                double dyDown = (hit.Y + hit.PreviewHeight) - moved.Y;
                double dyUp = (moved.Y + moved.PreviewHeight) - hit.Y;

                var best = new (double dx, double dy)[] {
            ( +dxRight + 1, 0 ),
            ( -(dxLeft)  - 1, 0 ),
            ( 0, +dyDown + 1 ),
            ( 0, -(dyUp)  - 1 )
        }.OrderBy(v => Math.Abs(v.dx) + Math.Abs(v.dy)).First();

                moved.X += best.dx;
                moved.Y += best.dy;

                moved.X = SnapTo(moved.X, grid);
                moved.Y = SnapTo(moved.Y, grid);

                ClampBlockInside(moved, st, moved.PreviewWidth, moved.PreviewHeight, headerHeight);
            }
        }

        private void AutoGrowStageIfNeeded(StageVm st, BlockVm b, double headerHeight)
        {
            double requiredW = b.X + b.PreviewWidth + 16;
            double requiredH = b.Y + b.PreviewHeight + 16 + headerHeight;
            if (requiredW > st.W) st.W = requiredW;
            if (requiredH > st.H) st.H = requiredH;
        }

        // === Přesun bloku NA cílovou pozici (přirozený drag) ===
        public void MoveBlockTo(BlockVm b, StageVm st, double targetX, double targetY, double grid, double headerHeight)
        {
            if (b == null || st == null) return;

            // 1) nastav přesnou cílovou pozici (před snapem)
            b.X = targetX;
            b.Y = targetY;

            // 2) clamp dovnitř stage
            ClampBlockInside(b, st, b.PreviewWidth, b.PreviewHeight, headerHeight);

            // 3) snap
            b.X = SnapTo(b.X, grid);
            b.Y = SnapTo(b.Y, grid);

            // 4) no-overlap
            ResolveBlockOverlaps(b, st, grid, headerHeight);

            // 5) autogrow stage
            AutoGrowStageIfNeeded(st, b, headerHeight);
        }

        // === Vložení „pod poslední“ ===
        public (double X, double Y) GetNextBelowLast(StageVm st, double left = 8, double margin = 12)
        {
            if (st.Blocks.Count == 0) return (left, margin);

            // najdi nejnižší blok
            var last = st.Blocks.OrderByDescending(b => b.Y + b.PreviewHeight).First();
            var y = last.Y + last.PreviewHeight + margin;

            // šířku vezmi 0 – bude řešit Clamp/AutoGrow po vložení
            return (left, y);
        }


        // ---- JSON helpers ----
        private static bool TryProp(JsonElement el, string propName, out JsonElement val)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(propName) || string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    val = p.Value; return true;
                }
            }
            val = default; return false;
        }

        private static string? TryGetString(JsonElement el, string prop)
        {
            return TryProp(el, prop, out var v)
                ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
                : null;
        }

        private static double? TryGetDouble(JsonElement el, string prop)
        {
            if (!TryProp(el, prop, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetDouble(out var d) ? d : (double?)null,
                JsonValueKind.String => double.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                                                        System.Globalization.CultureInfo.InvariantCulture, out var d2) ? d2 : (double?)null,
                _ => null
            };
        }

        // ---- Brush helper (bez pádu na špatný formát) ----
        private static Brush? ToBrushSafe(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            try
            {
                var obj = new BrushConverter().ConvertFromString(hex);
                if (obj is Brush br) return br;
            }
            catch { /* ignore */ }
            return null;
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
                System.Text.Json.JsonSerializer.Serialize(pins, new JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.WriteAllText(System.IO.Path.Combine(path, "editor_stage_routes.json"),
                System.Text.Json.JsonSerializer.Serialize(routes, new JsonSerializerOptions { WriteIndented = true }));
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

        private string _startMode = "Manual"; // Manual | AutoOnPrevComplete | AutoOnCondition
        public string StartMode { get => _startMode; set { _startMode = value; Raise(); } }

        private int _slaHours = 0; // 0 = bez SLA
        public int SLAHours { get => _slaHours; set { _slaHours = value; Raise(); } }

        private bool _allowReopen = true;
        public bool AllowReopen { get => _allowReopen; set { _allowReopen = value; Raise(); } }

        private bool _autoCompleteOnAllBlocks = false;
        public bool AutoCompleteOnAllBlocks { get => _autoCompleteOnAllBlocks; set { _autoCompleteOnAllBlocks = value; Raise(); } }

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

        // >>> NOVĚ: renderujeme přímo přes modelové komponenty
        public ObservableCollection<FieldComponentBase> Components { get; } = new();

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
