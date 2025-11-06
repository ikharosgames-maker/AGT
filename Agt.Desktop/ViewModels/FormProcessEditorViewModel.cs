using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Agt.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel pro editor procesu – doplněny overloady a metody tak,
    /// aby odpovídaly voláním z View (FormProcessEditorWindow.xaml.cs, StageEdgePathConverter).
    /// </summary>
    public sealed class FormProcessEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public EditorGraph Graph { get; } = new();

        private StageVm? _selectedStage;
        public StageVm? SelectedStage { get => _selectedStage; private set { _selectedStage = value; Raise(); } }

        private StageEdgeVm? _selectedStageEdge;
        public StageEdgeVm? SelectedStageEdge { get => _selectedStageEdge; private set { _selectedStageEdge = value; Raise(); } }

        public ObservableCollection<string> AvailableUsers { get; } = new();
        public ObservableCollection<string> AvailableGroups { get; } = new();

        // ====== Konstruktory ======
        public FormProcessEditorViewModel() { }

        // View volá: new FormProcessEditorViewModel(save, clone, registry)
        public FormProcessEditorViewModel(object? save, object? clone, object? registry) : this() { }

        // ====== Paleta / knihovna bloků ======
        public void LoadPaletteFromLibrary()
        {
            if (AvailableUsers.Count == 0)
            {
                AvailableUsers.Add(Environment.UserName);
                AvailableUsers.Add("demo-user");
            }
            if (AvailableGroups.Count == 0)
            {
                AvailableGroups.Add("Administrators");
                AvailableGroups.Add("Operators");
            }
        }
        public void LoadPaletteFromLibrary(object? lib) => LoadPaletteFromLibrary();
        // (Pokud v projektu existuje IBlockLibrary, klidně přidej public void LoadPaletteFromLibrary(IBlockLibrary lib) => LoadPaletteFromLibrary();

        // ====== Výběry ======
        public void ClearSelection() { SelectedStage = null; SelectedStageEdge = null; }
        public void SelectStage(StageVm s) { SelectedStage = s; SelectedStageEdge = null; }
        public void SelectEdge(StageEdgeVm e) { SelectedStage = null; SelectedStageEdge = e; }

        // ====== Stage API ======
        public StageVm AddStageAuto(double x, double y, double w = 520, double h = 380)
        {
            var st = new StageVm { Id = Guid.NewGuid(), X = x, Y = y, W = w, H = h };
            Graph.Stages.Add(st);
            return st;
        }

        // View volá i bez parametrů
        public StageVm AddStageAuto()
        {
            var (x, y) = Graph.Stages.Count == 0
                ? (0.0, 0.0)
                : (Graph.Stages.Max(s => s.X + s.W) + 40.0, Graph.Stages.Max(s => s.Y));
            return AddStageAuto(x, y, 520, 380);
        }

        public StageEdgeVm AddStageEdge(Guid fromStageId, Guid toStageId)
        {
            var e = new StageEdgeVm { Id = Guid.NewGuid(), FromStageId = fromStageId, ToStageId = toStageId };
            Graph.StageEdges.Add(e);
            return e;
        }

        // View občas posílá přímo StageVm → přidáme overload
        public StageEdgeVm AddStageEdge(StageVm from, StageVm to)
            => AddStageEdge(from.Id, to.Id);

        public StageVm? FindStage(Guid id) => Graph.Stages.FirstOrDefault(s => s.Id == id);

        // Hit-test bez radiusu
        public StageVm? HitTestStage(Point pCanvas)
            => Graph.Stages.LastOrDefault(s =>
                    pCanvas.X >= s.X && pCanvas.X <= s.X + s.W &&
                    pCanvas.Y >= s.Y && pCanvas.Y <= s.Y + s.H);

        // View volá: HitTestStage(pCanvas, 4)
        public StageVm? HitTestStage(Point pCanvas, double radius) => HitTestStage(pCanvas);

        // ====== Porty pro hrany (StageEdgePathConverter) ======
        // Out port = pravý střed stage (odchozí šipka)
        public Point GetStageOutPortAbs(StageVm stage)
            => new Point(stage.X + stage.W, stage.Y + stage.H / 2.0);

        public Point GetStageOutPortAbs(Guid stageId)
        {
            var st = FindStage(stageId);
            return st is null ? new Point() : GetStageOutPortAbs(st);
        }

        // In port = levý střed stage (příchozí šipka)
        public Point GetStageInPortAbs(StageVm stage)
            => new Point(stage.X, stage.Y + stage.H / 2.0);

        public Point GetStageInPortAbs(Guid stageId)
        {
            var st = FindStage(stageId);
            return st is null ? new Point() : GetStageInPortAbs(st);
        }

        // ====== Block API ======
        public (double X, double Y) GetNextBlockPosition(StageVm stage, double localX, double localY)
        {
            var x = Snap(localX, 8, noSnap: false);
            var y = Snap(localY, 8, noSnap: false);
            return (Clamp(x, 0, Math.Max(0, stage.W - 260)),
                    Clamp(y, 36, Math.Max(36, stage.H - 140)));
        }

        public BlockVm AddBlock(StageVm stage, Guid blockId, string title, string version, double x, double y)
        {
            var b = new BlockVm
            {
                Id = Guid.NewGuid(),
                RefBlockId = blockId,
                Title = title,
                Version = version,
                X = x,
                Y = y,
                PreviewWidth = 260,
                PreviewHeight = 140
            };
            stage.Blocks.Add(b);
            return b;
        }

        public void GeneratePreview(BlockVm b)
        {
            if (b.PreviewWidth <= 0) b.PreviewWidth = 260;
            if (b.PreviewHeight <= 0) b.PreviewHeight = 140;
        }

        public (double X, double Y) FindNearestFreeSlot(StageVm stage, double localX, double localY, double blockW, double blockH, int grid, double header)
        {
            var x = Snap(localX, grid, noSnap: false);
            var y = Snap(localY, grid, noSnap: false);
            int tries = 0;
            while (IntersectsAny(stage, x, y, blockW, blockH) && tries < 200)
            {
                x += grid;
                if (x + blockW > stage.W) { x = 0; y += grid; }
                if (y + blockH > stage.H) { y = header; }
                tries++;
            }
            x = Clamp(x, 0, Math.Max(0, stage.W - blockW));
            y = Clamp(y, header, Math.Max(header, stage.H - blockH));
            return (x, y);
        }

        private static bool IntersectsAny(StageVm stage, double x, double y, double w, double h)
            => stage.Blocks.Any(o => RectIntersects(x, y, w, h, o));

        public static bool RectIntersects(double x, double y, double w, double h, BlockVm other)
            => !(x + w <= other.X || other.X + other.PreviewWidth <= x ||
                 y + h <= other.Y || other.Y + other.PreviewHeight <= y);

        // Původní signatura (grid int)
        public void MoveBlockTo(BlockVm b, StageVm stage, double x, double y, int grid, double headerHeight)
        {
            x = Snap(x, grid, noSnap: false);
            y = Snap(y, grid, noSnap: false);
            x = Clamp(x, 0, Math.Max(0, stage.W - b.PreviewWidth));
            y = Clamp(y, headerHeight, Math.Max(headerHeight, stage.H - b.PreviewHeight));
            b.X = x; b.Y = y;
        }

        // Overload pro grid double
        public void MoveBlockTo(BlockVm b, StageVm stage, double x, double y, double grid, double headerHeight)
            => MoveBlockTo(b, stage, x, y, (int)Math.Round(grid), headerHeight);

        // Původní signatura
        public void ClampBlockInside(BlockVm b, StageVm stage, int grid, double headerHeight)
        {
            var x = Clamp(b.X, 0, Math.Max(0, stage.W - b.PreviewWidth));
            var y = Clamp(b.Y, headerHeight, Math.Max(headerHeight, stage.H - b.PreviewHeight));
            b.X = Snap(x, grid, noSnap: false);
            b.Y = Snap(y, grid, noSnap: false);
        }

        // Overload grid double
        public void ClampBlockInside(BlockVm b, StageVm stage, double grid, double headerHeight)
            => ClampBlockInside(b, stage, (int)Math.Round(grid), headerHeight);

        // Overload s blockW/H – volá View při přetahování
        public void ClampBlockInside(BlockVm b, StageVm stage, double blockW, double blockH, double headerHeight)
        {
            var x = Clamp(b.X, 0, Math.Max(0, stage.W - blockW));
            var y = Clamp(b.Y, headerHeight, Math.Max(headerHeight, stage.H - blockH));
            b.X = Snap(x, 8, noSnap: false);
            b.Y = Snap(y, 8, noSnap: false);
        }

        // ====== Snap/Helpers ======
        public double Snap(double value, int grid, bool noSnap)
            => noSnap || grid <= 1 ? value : Math.Round(value / grid) * grid;
        public double Snap(double value, double grid, bool noSnap)
            => Snap(value, (int)Math.Round(grid), noSnap);

        public (double X, double Y) SnapAll(double x, double y, int grid, bool noSnap)
            => (Snap(x, grid, noSnap), Snap(y, grid, noSnap));
        public (double X, double Y) SnapAll(double x, double y, double grid, bool noSnap)
            => SnapAll(x, y, (int)Math.Round(grid), noSnap);

        // View volá: vm.SnapAll(GridSize);
        public void SnapAll(double grid)
        {
            foreach (var st in Graph.Stages)
                foreach (var b in st.Blocks)
                {
                    b.X = Snap(b.X, grid, noSnap: false);
                    b.Y = Snap(b.Y, grid, noSnap: false);
                    b.X = Clamp(b.X, 0, Math.Max(0, st.W - b.PreviewWidth));
                    b.Y = Clamp(b.Y, 36, Math.Max(36, st.H - b.PreviewHeight));
                }
        }

        private static double Clamp(double v, double min, double max)
            => v < min ? min : (v > max ? max : v);

        public void SelectBlock(BlockVm b)
        {
            SelectedStage = Graph.Stages.FirstOrDefault(s => s.Blocks.Contains(b));
        }

        // ====== Publish ======
        public void Publish()
        {
            // TODO: vlastní logika publikace návrhu procesu.
        }
    }

    // ====== Model grafu/stagí/hran ======
    public sealed class EditorGraph
    {
        public ObservableCollection<StageVm> Stages { get; } = new();
        public ObservableCollection<StageEdgeVm> StageEdges { get; } = new();
    }

    public sealed class StageVm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; }
        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }
        private double _w = 520; public double W { get => _w; set { _w = value; Raise(); } }
        private double _h = 380; public double H { get => _h; set { _h = value; Raise(); } }

        public ObservableCollection<BlockVm> Blocks { get; } = new();
        public ObservableCollection<string> AssignedUsers { get; } = new();
        public ObservableCollection<string> AssignedGroups { get; } = new();
    }

    public sealed class BlockVm : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; }
        public Guid RefBlockId { get; set; }
        public string Title { get; set; } = "";
        public string Version { get; set; } = "1.0.0";

        private double _x; public double X { get => _x; set { _x = value; Raise(); } }
        private double _y; public double Y { get => _y; set { _y = value; Raise(); } }

        private double _pw = 260; public double PreviewWidth { get => _pw; set { _pw = value; Raise(); } }
        private double _ph = 140; public double PreviewHeight { get => _ph; set { _ph = value; Raise(); } }
    }

    public sealed class StageEdgeVm
    {
        public Guid Id { get; set; }
        public Guid FromStageId { get; set; }
        public Guid ToStageId { get; set; }
    }
}
