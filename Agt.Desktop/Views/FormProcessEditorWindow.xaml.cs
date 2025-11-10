using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
// --- USING DIREKTIVY (doplněné a sjednocené) ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Dostupnost ViewModelů a modelů:
using Agt.Desktop.ViewModels;   // obsahuje StageVm, BlockVm (případně jiné VM)
using Agt.Desktop.Models;       // obsahuje PaletteItem, FieldComponentBase atd.

// Alias krátkých názvů (zabrání CS0246 i když ve zbytku souboru používáš krátké typy):
using StageVm = Agt.Desktop.ViewModels.StageVm;
using BlockVm = Agt.Desktop.ViewModels.BlockVm;
using PaletteItem = Agt.Desktop.Models.PaletteItem;

namespace Agt.Desktop.Views
{
    public partial class FormProcessEditorWindow : Window
    {
        private const double GridSize = 10.0;
        private const double HeaderHeight = 36;
        private const double PortOffset = 12.0;
        private const double BlockW = 260.0;
        private const double BlockH = 140.0;

        private Point? _dragStartCanvas;
        private bool _isDraggingBlock;
        private Point _dragStartOnStage;
        private Vector _dragOffset;
        private BlockVm? _dragBlock;
        private StageVm? _dragStage;

        private FormProcessEditorViewModel? Vm => DataContext as FormProcessEditorViewModel;
        private bool AllowEdgeHit => !_linking && !_isDraggingBlock && !_paletteDragging && (PaletteList?.SelectedItem == null);

        private StageVm? FindOwningStage(BlockVm b)
            => Vm?.Graph?.Stages?.FirstOrDefault(s => s.Blocks.Contains(b));

        private bool _linking;
        private StageVm? _fromStage;
        private Point _rubberStart;
        private StageVm? _snapTargetStage;
        private const double SnapRadius = 18.0;
        private Ellipse? _snapHalo;

        private readonly ScaleTransform _scale = new();
        private readonly TranslateTransform _translate = new();

        private bool _paletteDragging;

        private DateTime _lastEdgeRedraw = DateTime.MinValue;
        private const int EdgeRedrawMinMs = 16;

        public FormProcessEditorWindow()
        {
            InitializeComponent();

            // VM z DI
            var sp = Agt.Desktop.App.Services;
            var save = sp.GetService<IFormSaveService>();
            var clone = sp.GetService<IFormCloneService>();
            var registry = sp.GetService<IFormCaseRegistryService>();
            DataContext = new FormProcessEditorViewModel(save, clone, registry);


            var group = new TransformGroup();
            group.Children.Add(_scale);
            group.Children.Add(_translate);
            RootCanvas.RenderTransform = group;

            var vm = (FormProcessEditorViewModel)DataContext;
            var lib = Agt.Desktop.Services.BlockLibraryJson.Default;   // typ = Agt.Desktop.Services.IBlockLibrary
            vm.LoadPaletteFromLibrary(lib);
            vm.Graph.StageEdges.CollectionChanged += (_, __) => RedrawEdges(force: true);
            vm.Graph.Stages.CollectionChanged += OnStagesChanged;
            // ZAJISTÍ, že vždy dostaneme myš i když Canvas nemá barvu:
            RootCanvas.Background = Brushes.Transparent;

            // Guma nesmí brát události:
            RubberPath.IsHitTestVisible = false;

            // Globální pojistka pro MouseUp (když by něco spolykalo MouseUp na Canvasu):
            PreviewMouseLeftButtonUp += FormProcessEditorWindow_PreviewMouseLeftButtonUp;

            // Volitelné: když by došlo ke ztrátě capture (alt-tab apod.), tak uklidit:
            LostMouseCapture += FormProcessEditorWindow_LostMouseCapture;

            PreviewKeyDown += FormProcessEditorWindow_PreviewKeyDown;
        }
        private void FormProcessEditorWindow_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (!_linking) return;
            var pos = ToCanvas(e.GetPosition(ZoomHost));
            FinishLinkingAt(pos);
            e.Handled = true;
        }

        private void FormProcessEditorWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _linking)
            {
                CancelLinking();
                e.Handled = true;
            }
        }

        private Point ToCanvas(Point viewPoint)
        {
            var m = RootCanvas.RenderTransform.Value;
            if (m.HasInverse)
            {
                m.Invert();
                return m.Transform(viewPoint);
            }
            return viewPoint;
        }

        private void OnStagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var s in e.NewItems.OfType<StageVm>())
                    s.PropertyChanged += (_, __) => RedrawEdges();
            }
            RedrawEdges(force: true);
        }

        // Paleta
        private void PaletteList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_paletteDragging && PaletteList.SelectedItem is PaletteItem)
            {
                _paletteDragging = true;
                DragDrop.DoDragDrop(PaletteList, PaletteList.SelectedItem, DragDropEffects.Copy);
                _paletteDragging = false;
            }
        }

        private void Stage_Body_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(PaletteItem)))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private BlockVm PlaceBlockInStage(StageVm stage, PaletteItem palItem, double localX = 0, double localY = 0)
        {
            var vm = (FormProcessEditorViewModel)DataContext;

            var (x, y) = vm.GetNextBlockPosition(stage, localX, localY);
            var b = vm.AddBlock(stage, palItem.BlockId, palItem.Name, palItem.Version, x, y);

            vm.GeneratePreview(b);

            var (nx, ny) = vm.FindNearestFreeSlot(stage, localX, localY, b.PreviewWidth, b.PreviewHeight, grid: 8, header: 36);
            vm.MoveBlockTo(b, stage, nx, ny, grid: 8, headerHeight: 0);

            vm.SelectBlock(b);
            if (vm.SelectedStage == null) vm.SelectStage(stage);
            return b;
        }

        private void OnPaletteDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            if (PaletteList.SelectedItem is not PaletteItem sel) return;

            if (!vm.Graph.Stages.Any())
            {
                vm.AddStageAuto();
            }

            var st = vm.Graph.Stages.First();
            var (x, y) = vm.GetNextBlockPosition(st, 100, 20);
            var b = PlaceBlockInStage(st, sel, x, y);
            vm.GeneratePreview(b);
            RedrawEdges(force: true);
            PaletteList.SelectedItem = null;
        }



        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;

            if (PaletteList.SelectedItem is PaletteItem sel)
            {
                var pCanvas = ToCanvas(e.GetPosition(ZoomHost));
                var st = vm.HitTestStage(pCanvas, 4);
                if (st != null)
                {
                    var local = new Point(pCanvas.X - st.X, pCanvas.Y - st.Y - HeaderHeight);
                    if (local.Y < 0) local.Y = 0;

                    var x = vm.Snap(local.X, GridSize, noSnap: false);
                    var y = vm.Snap(local.Y, GridSize, noSnap: false);
                    x = Math.Max(0, Math.Min(x, st.W - BlockW));
                    y = Math.Max(0, Math.Min(y, st.H - HeaderHeight - BlockH));

                    var b = PlaceBlockInStage(st, sel, x, y);
                    vm.GeneratePreview(b);
                    PaletteList.SelectedItem = null;
                    return;
                }
            }

            vm.ClearSelection();

            if (_linking) CancelLinking();
        }

        private void StageHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is StageVm stage)
            {
                ((FormProcessEditorViewModel)DataContext).SelectStage(stage);
                RefreshPickers();
                e.Handled = true;
            }
        }

        private void Stage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var stage = (sender as FrameworkElement)?.DataContext as StageVm;
            if (stage != null)
            {
                ((FormProcessEditorViewModel)DataContext).SelectStage(stage);
                RefreshPickers();
            }

            _dragStartCanvas = ToCanvas(e.GetPosition(ZoomHost));
            _dragStage = stage;
            (sender as FrameworkElement)?.CaptureMouse();
            e.Handled = true;
        }

        private void Stage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragStage is null || _dragStartCanvas is null || e.LeftButton != MouseButtonState.Pressed) return;

            var p = ToCanvas(e.GetPosition(ZoomHost));
            var dx = p.X - _dragStartCanvas.Value.X;
            var dy = p.Y - _dragStartCanvas.Value.Y;

            _dragStage.X += dx;
            _dragStage.Y += dy;

            _dragStartCanvas = p;

            RedrawEdges();
        }

        private void Stage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as FrameworkElement)?.ReleaseMouseCapture();

            if (_dragStage != null)
            {
                var vm = (FormProcessEditorViewModel)DataContext;
                _dragStage.X = vm.Snap(_dragStage.X, GridSize, false);
                _dragStage.Y = vm.Snap(_dragStage.Y, GridSize, false);
            }

            _dragStage = null;
            _dragStartCanvas = null;
            RedrawEdges(force: true);
            e.Handled = true;
        }

        private void Stage_Resize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var border = FindAncestor<Border>(sender as DependencyObject);
            if (border?.DataContext is StageVm st)
            {
                var vm = (FormProcessEditorViewModel)DataContext;

                var dx = e.HorizontalChange / _scale.ScaleX;
                var dy = e.VerticalChange / _scale.ScaleY;

                st.W = Math.Max(200, st.W + dx);
                st.H = Math.Max(150, st.H + dy);

                // místo fixních 260x140 (BlockW/BlockH) respektuj reálný preview rozměr každého bloku:
                foreach (var b in st.Blocks)
                    vm.ClampBlockInside(b, st, b.PreviewWidth, b.PreviewHeight, HeaderHeight);

                RedrawEdges();
            }
        }

        private static bool RectIntersects(double x, double y, double w, double h, BlockVm other)
        {
            return !(x + w <= other.X ||
                     other.X + other.PreviewWidth <= x ||
                     y + h <= other.Y ||
                     other.Y + other.PreviewHeight <= y);
        }

        private static double SnapTo(double v, double grid) => grid <= 0 ? v : Math.Round(v / grid) * grid;

        public (double X, double Y) FindNearestFreeSlot(StageVm st, double targetX, double targetY,
                                                        double blockW, double blockH,
                                                        double grid, double headerHeight,
                                                        int maxIterations = 200)
        {
            double baseX = SnapTo(targetX, grid);
            double baseY = SnapTo(targetY, grid);

            baseX = Math.Max(0, Math.Min(baseX, st.W - blockW));
            baseY = Math.Max(headerHeight, Math.Min(baseY, st.H - headerHeight - blockH));

            bool IsFree(double x, double y)
            {
                foreach (var other in st.Blocks)
                {
                    if (RectIntersects(x, y, blockW, blockH, other)) return false;
                }
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
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY - radius, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(headerHeight, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY + radius, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(headerHeight, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                }
                for (int dy = -radius + step; dy <= radius - step; dy += step)
                {
                    {
                        double x = SnapTo(baseX - radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(headerHeight, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                    {
                        double x = SnapTo(baseX + radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(headerHeight, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                }

                radius += step;
            }

            return (baseX, baseY);
        }

        private Point GetMouseOnStage(FrameworkElement blockElement)
        {
            var stageCanvas = FindParent<Canvas>(blockElement);
            return Mouse.GetPosition(stageCanvas);
        }

        private void OnBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is BlockVm b)
            {
                _dragBlock = b;
                _dragStage = Vm != null ? (Vm.SelectedStage ?? FindOwningStage(b)) : null;
                if (_dragStage == null) return;

                _isDraggingBlock = true;

                _dragStartOnStage = GetMouseOnStage(fe);

                _dragOffset = (Vector)(_dragStartOnStage - new Point(b.X, b.Y));

                Mouse.Capture(fe);
                e.Handled = true;

                Vm?.SelectBlock(b);
                if (Vm?.SelectedStage == null) Vm?.SelectStage(_dragStage);
            }
        }

        private void OnBlockMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingBlock || _dragBlock is null || _dragStage is null || Vm is null) return;
            if (sender is not FrameworkElement fe) return;

            var p = GetMouseOnStage(fe);

            var target = p - _dragOffset;

            Vm.MoveBlockTo(_dragBlock, _dragStage, target.X, target.Y, grid: 8, headerHeight: 0);
        }

        private void OnBlockMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBlock = false;
            _dragBlock = null;
            _dragStage = null;
            Mouse.Capture(null);
            e.Handled = true;
        }

        private void Stage_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(PaletteItem))) return;

            var vm = (FormProcessEditorViewModel)DataContext;
            var stage = (sender as FrameworkElement)?.DataContext as StageVm;
            if (stage == null) return;

            var posCanvas = ToCanvas(e.GetPosition(ZoomHost));
            var local = new Point(posCanvas.X - stage.X, posCanvas.Y - stage.Y - HeaderHeight);
            if (local.Y < 0) local.Y = 0;

            var item = (PaletteItem)e.Data.GetData(typeof(PaletteItem))!;
            var x = vm.Snap(local.X, GridSize, noSnap: false);
            var y = vm.Snap(local.Y, GridSize, noSnap: false);

            x = Math.Max(0, Math.Min(x, stage.W - BlockW));
            y = Math.Max(0, Math.Min(y, stage.H - HeaderHeight - BlockH));

            var b = PlaceBlockInStage(stage, item, x, y);
            vm.GeneratePreview(b);
            PaletteList.SelectedItem = null;
        }

        private void Stage_OutPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            _fromStage = (sender as FrameworkElement)?.DataContext as StageVm;
            if (_fromStage is null) return;

            _linking = true;

            var s = GetStageOutPortAbs(_fromStage);
            _rubberStart = new Point(s.X, s.Y);

            RubberPath.Data = new PathGeometry(new[] { new PathFigure { StartPoint = _rubberStart } });
            RubberPath.Visibility = Visibility.Visible;

            // KLÍČOVÉ: aby vždy dorazily Move/Up i mimo Canvas/okno:
            RootCanvas.CaptureMouse();

            Cursor = Cursors.Cross;
            e.Handled = true;

            // při linkování vypneme hit-test hran (lepší chytání portů)
            RedrawEdges(force: true);
        }

        private void Stage_InPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }


        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_linking)
            {
                var vm = (FormProcessEditorViewModel)DataContext;
                var mouseCanvas = ToCanvas(e.GetPosition(ZoomHost));

                StageVm? bestStage = null;
                double bestDist = double.MaxValue;
                Point bestPort = new();

                foreach (var st in vm.Graph.Stages)
                {
                    if (_fromStage != null && st.Id == _fromStage.Id) continue;
                    var port = GetStageInPortAbs(st);
                    var dist = Distance(mouseCanvas, new Point(port.X, port.Y));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestStage = st;
                        bestPort = new Point(port.X, port.Y);
                    }
                }

                Point endPoint;
                if (bestStage != null && bestDist <= SnapRadius)
                {
                    endPoint = bestPort;
                    ShowSnapHalo(bestPort);
                    _snapTargetStage = bestStage;
                }
                else
                {
                    endPoint = mouseCanvas;
                    HideSnapHalo();
                    _snapTargetStage = null;
                }

                var p1 = _rubberStart;
                var p2 = endPoint;
                var dx = Math.Max(60, Math.Abs(p2.X - p1.X) * 0.5);
                var c1 = new Point(p1.X + dx, p1.Y);
                var c2 = new Point(p2.X - dx, p2.Y);

                var fig = new PathFigure { StartPoint = p1 };
                fig.Segments.Add(new BezierSegment(c1, c2, p2, true));

                var geo = new PathGeometry();
                geo.Figures.Add(fig);
                RubberPath.Data = geo;
            }
        }
        private void FinishLinkingAt(Point mouseCanvas)
        {
            if (!_linking || _fromStage is null) { CancelLinking(); return; }

            var vm = (FormProcessEditorViewModel)DataContext;

            // Pokud není aktuální snap cíl, zkusíme naposledy dopočítat z pozice myši:
            StageVm? target = _snapTargetStage;
            if (target == null)
            {
                double bestDist = double.MaxValue;
                StageVm? best = null;
                foreach (var st in vm.Graph.Stages)
                {
                    if (_fromStage.Id == st.Id) continue;
                    var port = GetStageInPortAbs(st);
                    var dist = Distance(mouseCanvas, new Point(port.X, port.Y));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = st;
                    }
                }
                if (best != null && bestDist <= SnapRadius) target = best;
            }

            if (target != null && target.Id != _fromStage.Id)
            {
                vm.AddStageEdge(_fromStage, target);
                vm.SelectEdge(vm.Graph.StageEdges.Last());
                RedrawEdges(force: true);
            }

            CancelLinking();
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_linking) return;
            var pos = ToCanvas(e.GetPosition(ZoomHost));
            FinishLinkingAt(pos);
            e.Handled = true;
        }
        private void FormProcessEditorWindow_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_linking)
                CancelLinking();
        }
        private void CancelLinking()
        {
            _linking = false;
            _fromStage = null;
            _snapTargetStage = null;

            RubberPath.Visibility = Visibility.Collapsed;
            RubberPath.Data = null;

            HideSnapHalo();
            Cursor = Cursors.Arrow;

            if (Mouse.Captured == RootCanvas)
                Mouse.Capture(null);

            // po ukončení linkování hranám vrátíme hit-test:
            RedrawEdges(force: true);
        }


        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void HideSnapHalo()
        {
            if (_snapHalo != null)
            {
                EdgeLayer.Children.Remove(_snapHalo);
                _snapHalo = null;
            }
        }

        private static readonly Brush PortNormalBrush =
            new SolidColorBrush(Color.FromRgb(0x5A, 0xAF, 0xFF));
        private static readonly Brush PortHoverBrush = Brushes.Gold;

        private void Port_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Shape sh)
            {
                sh.Fill = PortHoverBrush;
                // předsaď port nad hrany, aby šel vždy chytit
                Panel.SetZIndex(sh, 5000);
            }
        }

        private void Port_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Shape sh)
            {
                sh.Fill = PortNormalBrush;
                // vrať standardní pořadí, ať hrany lze vybírat mimo interakci
                Panel.SetZIndex(sh, 0);
            }
        }

        private void ShowSnapHalo(Point center)
        {
            if (_snapHalo == null)
            {
                _snapHalo = new Ellipse
                {
                    Width = SnapRadius * 2,
                    Height = SnapRadius * 2,
                    Stroke = Brushes.Gold,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 255, 215, 0)),
                    IsHitTestVisible = false // NEblokovat kliky v okolí portu
                };
                EdgeLayer.Children.Add(_snapHalo);
            }
            Canvas.SetLeft(_snapHalo, center.X - SnapRadius);
            Canvas.SetTop(_snapHalo, center.Y - SnapRadius);
            Panel.SetZIndex(_snapHalo, 2500);
        }

        private void RedrawEdges(bool force = false)
        {
            var now = DateTime.UtcNow;
            if (!force && (now - _lastEdgeRedraw).TotalMilliseconds < EdgeRedrawMinMs)
                return;
            _lastEdgeRedraw = now;

            var vm = (FormProcessEditorViewModel)DataContext;
            EdgeLayer.Children.Clear();

            foreach (var edge in vm.Graph.StageEdges)
            {
                var from = vm.FindStage(edge.FromStageId);
                var to = vm.FindStage(edge.ToStageId);
                if (from is null || to is null) continue;

                var p1 = GetStageOutPortAbs(from);
                var p2 = GetStageInPortAbs(to);

                var dx = Math.Max(60, Math.Abs(p2.X - p1.X) * 0.5);
                var c1 = new Point(p1.X + dx, p1.Y);
                var c2 = new Point(p2.X - dx, p2.Y);

                var fig = new PathFigure { StartPoint = new Point(p1.X, p1.Y) };
                fig.Segments.Add(new BezierSegment(c1, c2, new Point(p2.X, p2.Y), true));
                var geo = new PathGeometry();
                geo.Figures.Add(fig);

                var path = new System.Windows.Shapes.Path
                {
                    StrokeThickness = 2.0,
                    Stroke = Equals(vm.SelectedStageEdge, edge) ? Brushes.Orange : Brushes.LightSkyBlue,
                    Data = geo,
                    Cursor = Cursors.Hand,
                    // hit-test pouze když nic netaháme a ne-linkujeme:
                    IsHitTestVisible = AllowEdgeHit
                };

                path.Tag = edge.Id;
                path.MouseLeftButtonDown += Edge_MouseLeftButtonDown;

                // drž hrany nízko, aby porty/bloky mohly dostat klik
                Panel.SetZIndex(path, 1000);

                EdgeLayer.Children.Add(path);
            }
        }

        private void Edge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Shapes.Path p && p.Tag is Guid id)
            {
                var vm = (FormProcessEditorViewModel)DataContext;
                var edge = vm.Graph.StageEdges.FirstOrDefault(ed => ed.Id == id);
                if (edge != null)
                {
                    vm.SelectEdge(edge);
                    RedrawEdges(force: true);
                    e.Handled = true;
                }
            }
        }

        private (double X, double Y) GetStageOutPortAbs(StageVm s)
            => (s.X + s.W - PortOffset, s.Y + HeaderHeight + (s.H - HeaderHeight) / 2.0);

        private (double X, double Y) GetStageInPortAbs(StageVm s)
            => (s.X + PortOffset, s.Y + HeaderHeight + (s.H - HeaderHeight) / 2.0);

        private void ZoomHost_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var viewPos = e.GetPosition(ZoomHost);
            var canvasPos = ToCanvas(viewPos);

            var zoom = e.Delta > 0 ? .1 : -.1;
            var newScale = _scale.ScaleX + zoom;
            if (newScale < 0.2) return;

            _scale.ScaleX = _scale.ScaleY = newScale;

            _translate.X = -canvasPos.X * newScale + viewPos.X;
            _translate.Y = -canvasPos.Y * newScale + viewPos.Y;

            e.Handled = true;
        }

        private void ZoomHost_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _scale.ScaleX = _scale.ScaleY = 1.0;
            _translate.X = _translate.Y = 0.0;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null && child is not T)
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        // Jednoduché přesměrování na publikaci ve ViewModelu (Auto – PATCH/MINOR).
        // Pokud už máš v XAML Command bindingy, tenhle handler klidně odstraň.
        private void OnPublish(object sender, RoutedEventArgs e)
        {
            Vm?.Publish();
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();

        private void OnAddStage(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            vm.AddStageAuto(); // název i pozice automaticky
            RedrawEdges(force: true);
        }




        private void OnResetZoom(object sender, RoutedEventArgs e)
        {
            _scale.ScaleX = _scale.ScaleY = 1.0;
            _translate.X = _translate.Y = 0.0;
        }

        private void OnSnapAll(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            vm.SnapAll(GridSize);
            RedrawEdges(force: true);
        }

        // ======= Assignery (Skupiny/Users) =======
        private void OnAddGroups(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var st = vm.SelectedStage;
            if (st == null) return;

            var chosen = LbAvailGroups.SelectedItems.Cast<string>().ToList();
            foreach (var g in chosen)
                if (!st.AssignedGroups.Contains(g, StringComparer.OrdinalIgnoreCase))
                    st.AssignedGroups.Add(g);
            OnGroupFilterChanged(TbGroupFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void OnRemoveGroups(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var st = vm.SelectedStage;
            if (st == null) return;

            var chosen = LbAssignedGroups.SelectedItems.Cast<string>().ToList();
            foreach (var g in chosen)
                st.AssignedGroups.Remove(g);
            OnGroupFilterChanged(TbGroupFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void OnAddUsers(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var st = vm.SelectedStage;
            if (st == null) return;

            var chosen = LbAvailUsers.SelectedItems.Cast<string>().ToList();
            foreach (var u in chosen)
                if (!st.AssignedUsers.Contains(u, StringComparer.OrdinalIgnoreCase))
                    st.AssignedUsers.Add(u);
            OnUserFilterChanged(TbUserFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void OnRemoveUsers(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var st = vm.SelectedStage;
            if (st == null) return;

            var chosen = LbAssignedUsers.SelectedItems.Cast<string>().ToList();
            foreach (var u in chosen)
                st.AssignedUsers.Remove(u);
            OnUserFilterChanged(TbUserFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void OnGroupFilterChanged(object sender, TextChangedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var filter = TbGroupFilter.Text?.Trim() ?? "";
            IEnumerable<string> baseList = vm.AvailableGroups;

            if (!string.IsNullOrEmpty(filter))
                baseList = baseList.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase));

            if (vm.SelectedStage != null)
                baseList = baseList.Where(x => !vm.SelectedStage.AssignedGroups.Contains(x, StringComparer.OrdinalIgnoreCase));

            LbAvailGroups.ItemsSource = baseList.ToList();
        }

        private void OnUserFilterChanged(object sender, TextChangedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            var filter = TbUserFilter.Text?.Trim() ?? "";
            IEnumerable<string> baseList = vm.AvailableUsers;

            if (!string.IsNullOrEmpty(filter))
                baseList = baseList.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase));

            if (vm.SelectedStage != null)
                baseList = baseList.Where(x => !vm.SelectedStage.AssignedUsers.Contains(x, StringComparer.OrdinalIgnoreCase));

            LbAvailUsers.ItemsSource = baseList.ToList();
        }

        private void RefreshPickers()
        {
            OnGroupFilterChanged(TbGroupFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
            OnUserFilterChanged(TbUserFilter, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }
    }
}
