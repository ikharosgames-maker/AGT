using Agt.Desktop.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Agt.Desktop.Views
{
    public partial class FormProcessEditorWindow : Window
    {
        private const double GridSize = 10.0;
        private const double HeaderHeight = 34.0;
        private const double PortOffset = 12.0; // X-offset portu od kraje
        private const double BlockW = 260.0;
        private const double BlockH = 140.0;

        // drag posun
        private Point? _dragStartCanvas;
        private bool _isDraggingBlock;
        private Point _dragStartOnStage;       // pozice myši relativně ke stage při zahájení
        private Vector _dragOffset;            // offset kurzor -> levý horní roh bloku
        private BlockVm? _dragBlock;
        private StageVm? _dragStage;           // stage, do které blok patří

        private FormProcessEditorViewModel? Vm => DataContext as FormProcessEditorViewModel;

        private StageVm? FindOwningStage(BlockVm b)
            => Vm?.Graph?.Stages?.FirstOrDefault(s => s.Blocks.Contains(b));

        private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null && child is not T)
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        // linking (drag & drop)
        private bool _linking;
        private StageVm? _fromStage;
        private Point _rubberStart;
        private StageVm? _snapTargetStage;           // kandidát cíle (vstupní port)
        private const double SnapRadius = 18.0;      // „magnet“ na port
        private Ellipse? _snapHalo;                  // vizuální zvýraznění cílového portu

        // zoom/pan
        private readonly ScaleTransform _scale = new();
        private readonly TranslateTransform _translate = new();

        private bool _paletteDragging;

        // throttle pro RedrawEdges
        private DateTime _lastEdgeRedraw = DateTime.MinValue;
        private const int EdgeRedrawMinMs = 16; // ~60 FPS


        public FormProcessEditorWindow()
        {
            InitializeComponent();
            DataContext = new FormProcessEditorViewModel();

            var group = new TransformGroup();
            group.Children.Add(_scale);
            group.Children.Add(_translate);
            RootCanvas.RenderTransform = group;

            var vm = (FormProcessEditorViewModel)DataContext;
            var lib = Agt.Desktop.Services.BlockLibraryJson.Default;
            vm.LoadPaletteFromLibrary(lib);
            vm.Graph.StageEdges.CollectionChanged += (_, __) => RedrawEdges(force: true);
            vm.Graph.Stages.CollectionChanged += OnStagesChanged;

            // klávesové zkratky (ESC ruší linkování)
            PreviewKeyDown += FormProcessEditorWindow_PreviewKeyDown;
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

            // základní pozice (uvnitř stage)
            var (x, y) = vm.GetNextBlockPosition(stage, localX, localY);

            // přidej blok do stage
            var b = vm.AddBlock(stage, palItem.Key, palItem.Name, palItem.Version, x, y);

            // vygeneruj náhled z JSON knihovny
            vm.GeneratePreview(b);

            var (nx, ny) = vm.FindNearestFreeSlot(stage,
                                                     localX, localY,
                                                     b.PreviewWidth, b.PreviewHeight,
                                                     grid: 8, headerHeight: 36);

            // 4) Umísti blok přesně na nalezenou pozici
            vm.MoveBlockTo(b, stage, nx, ny, grid: 8, headerHeight: 36);

            // 5) Výběr
            vm.SelectBlock(b);
            if (vm.SelectedStage == null) vm.SelectStage(stage);

            return b;
        }

        private void OnPaletteDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            if (PaletteList.SelectedItem is not PaletteItem sel) return;

            if (!vm.Graph.Stages.Any())
                vm.AddStage("Stage 1", 100, 100, 600, 400);

            var st = vm.Graph.Stages.First();
            var (x, y) = vm.GetNextBlockPosition(st, 100, HeaderHeight + 20);
            var b = PlaceBlockInStage(st, sel, x, y);
            vm.GeneratePreview(b);
            RedrawEdges(force: true);
            PaletteList.SelectedItem = null;
        }

        // Canvas: vložení bloku (select&click) nebo clear výběru / zrušení linkování
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

            // klik do prázdna → zrušit výběr
            vm.ClearSelection();

            // zruš rozpracované linkování
            if (_linking) CancelLinking();
        }

        // Stage drag/select
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

            // živě bez snapu
            _dragStage.X += dx;
            _dragStage.Y += dy;

            _dragStartCanvas = p;

            RedrawEdges(); // throttled
        }

        private void Stage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            (sender as FrameworkElement)?.ReleaseMouseCapture();

            if (_dragStage != null)
            {
                var vm = (FormProcessEditorViewModel)DataContext;
                _dragStage.X = vm.Snap(_dragStage.X, GridSize, noSnap: false);
                _dragStage.Y = vm.Snap(_dragStage.Y, GridSize, noSnap: false);
            }

            _dragStage = null;
            _dragStartCanvas = null;
            RedrawEdges(force: true);
            e.Handled = true;
        }

        // Resize stage
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

                vm.ClampBlocksInside(st, BlockW, BlockH, HeaderHeight);
                RedrawEdges(); // throttled
            }
        }
        // Volné místo? – obdélník [x..x+w, y..y+h] nesmí kolidovat s jinými bloky
        private static bool RectIntersects(double x, double y, double w, double h, BlockVm other)
        {
            return !(x + w <= other.X ||
                     other.X + other.PreviewWidth <= x ||
                     y + h <= other.Y ||
                     other.Y + other.PreviewHeight <= y);
        }

        private static double SnapTo(double v, double grid) => grid <= 0 ? v : Math.Round(v / grid) * grid;

        /// <summary>Vrátí nejbližší volnou pozici ke zvolenému bodu (targetX/Y) pro blok o rozměru blockW/H.
        /// Respektuje snap na grid, clamp do stage a vyhne se kolizím. Pokud to nejde na pixel přesně, dělá "spirálu".
        /// </summary>
        public (double X, double Y) FindNearestFreeSlot(StageVm st, double targetX, double targetY,
                                                        double blockW, double blockH,
                                                        double grid, double headerHeight,
                                                        int maxIterations = 200)
        {
            // 1) snap cílového bodu
            double baseX = SnapTo(targetX, grid);
            double baseY = SnapTo(targetY, grid);

            // 2) omezit do stage (základní clamp)
            baseX = Math.Max(0, Math.Min(baseX, st.W - blockW));
            baseY = Math.Max(0, Math.Min(baseY, st.H - headerHeight - blockH));

            // 3) rychlý check — když to hned sedí, hotovo
            bool IsFree(double x, double y)
            {
                foreach (var other in st.Blocks)
                {
                    // při vkládání nový blok ještě není v kolekci → žádný self-compare netřeba
                    if (RectIntersects(x, y, blockW, blockH, other)) return false;
                }
                return true;
            }
            if (IsFree(baseX, baseY)) return (baseX, baseY);

            // 4) spirála kolem cílového bodu: posuny v osách s postupně rostoucím poloměrem
            //    krok = grid (nebo 8 px), aby to lícovalo se snapem
            var step = Math.Max(1, (int)(grid > 0 ? grid : 8));
            int radius = step;
            int iter = 0;

            while (iter < maxIterations && radius < 4000)
            {
                // prohledáme „čtverec“ v daném radiusu po mřížce
                for (int dx = -radius; dx <= radius; dx += step)
                {
                    // horní hrana
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY - radius, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(0, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                    // dolní hrana
                    {
                        double x = SnapTo(baseX + dx, grid);
                        double y = SnapTo(baseY + radius, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(0, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                }
                for (int dy = -radius + step; dy <= radius - step; dy += step)
                {
                    // levá hrana
                    {
                        double x = SnapTo(baseX - radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(0, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                    // pravá hrana
                    {
                        double x = SnapTo(baseX + radius, grid);
                        double y = SnapTo(baseY + dy, grid);
                        x = Math.Max(0, Math.Min(x, st.W - blockW));
                        y = Math.Max(0, Math.Min(y, st.H - headerHeight - blockH));
                        if (IsFree(x, y)) return (x, y);
                        iter++;
                        if (iter >= maxIterations) break;
                    }
                }

                radius += step;
            }

            // 5) nouzově vrať základ – na něm si to pak vyřeší MoveBlockTo (odtlačení)
            return (baseX, baseY);
        }

        private Point GetMouseOnStage(FrameworkElement blockElement)
        {
            // Najdi Canvas, na kterém jsou vykreslené bloky uvnitř Stage (je to ItemsPanel Canvas)
            // V praxi stačí vzít nejbližší parent Canvas z vizuálního stromu:
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

                // myš relativně ke canvasu stagi
                _dragStartOnStage = GetMouseOnStage(fe);

                // offset kurzor -> levý horní roh bloku (aby drag kopíroval polohu myši)
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

            // aktuální kurzor na stage canvasu
            var p = GetMouseOnStage(fe);

            // cílová pozice = kurzor - offset
            var target = p - _dragOffset;

            // plynulý přirozený pohyb: požádej VM, ať umístí blok přesně na target (se snapem/kolizemi/clampe)
            Vm.MoveBlockTo(_dragBlock, _dragStage, target.X, target.Y, grid: 8, headerHeight: 36);
        }

        private void OnBlockMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBlock = false;
            _dragBlock = null;
            _dragStage = null;
            Mouse.Capture(null);
            e.Handled = true;
        }


        // Drop z palety
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

        // ====== LINKING: Drag & Drop s magnetem na IN port ======

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

            Cursor = Cursors.Cross;
            e.Handled = true;
        }

        private void Stage_InPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // nevytváříme na klik – používáme drag&drop
            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // update „gumové“ linky + magnet na in port
            if (_linking)
            {
                var vm = (FormProcessEditorViewModel)DataContext;
                var mouseCanvas = ToCanvas(e.GetPosition(ZoomHost));

                // najdi nejbližší IN port (kromě zdroje)
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

                // jestli je blízko → přichytit + halo highlight
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

                // zkonstruuj Bezier
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

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_linking || _fromStage is null) return;

            var vm = (FormProcessEditorViewModel)DataContext;

            // preferuj „magnetem“ vybraný cíl
            if (_snapTargetStage != null)
            {
                vm.AddStageEdge(_fromStage, _snapTargetStage);
                vm.SelectEdge(vm.Graph.StageEdges.Last());
                RedrawEdges(force: true);
            }

            CancelLinking();
        }

        private void CancelLinking()
        {
            _linking = false;
            _fromStage = null;
            RubberPath.Visibility = Visibility.Collapsed;
            HideSnapHalo();
            Cursor = Cursors.Arrow;
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
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
                    Fill = new SolidColorBrush(Color.FromArgb(40, 255, 215, 0))
                };
                EdgeLayer.Children.Add(_snapHalo);
            }
            Canvas.SetLeft(_snapHalo, center.X - SnapRadius);
            Canvas.SetTop(_snapHalo, center.Y - SnapRadius);
            Panel.SetZIndex(_snapHalo, 2500);
        }

        private void HideSnapHalo()
        {
            if (_snapHalo != null)
            {
                EdgeLayer.Children.Remove(_snapHalo);
                _snapHalo = null;
            }
        }

        // ==== Port hover ====
        private static readonly Brush PortNormalBrush =
            new SolidColorBrush(Color.FromRgb(0x5A, 0xAF, 0xFF)); // #5AF
        private static readonly Brush PortHoverBrush = Brushes.Gold;

        private void Port_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Shape sh) sh.Fill = PortHoverBrush;
        }

        private void Port_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Shape sh) sh.Fill = PortNormalBrush;
        }

        // HRANY (kliknutelné Path do EdgeLayer)
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

                var path = new Path
                {
                    StrokeThickness = 2.0,
                    Stroke = Equals(vm.SelectedStageEdge, edge) ? Brushes.Orange : Brushes.LightSkyBlue,
                    Data = geo,
                    Cursor = Cursors.Hand,
                    IsHitTestVisible = true
                };

                path.Tag = edge.Id;
                path.MouseLeftButtonDown += Edge_MouseLeftButtonDown;

                EdgeLayer.Children.Add(path);
            }
        }

        private void Edge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Path p && p.Tag is Guid id)
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

        // Port pozice: střed celé výšky stage
        private (double X, double Y) GetStageOutPortAbs(StageVm s)
            => (s.X + s.W - PortOffset, s.Y + s.H / 2.0);

        private (double X, double Y) GetStageInPortAbs(StageVm s)
            => (s.X + PortOffset, s.Y + s.H / 2.0);

        // Zoom/Pan
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

        // Helpers
        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void OnPublish(object sender, RoutedEventArgs e) => MessageBox.Show("TODO: Publish (repo napojíme).");
        private void OnClose(object sender, RoutedEventArgs e) => Close();
        private void OnAddStage(object sender, RoutedEventArgs e)
        {
            var vm = (FormProcessEditorViewModel)DataContext;
            vm.AddStage("Nová Stage", 100, 100, 600, 400);
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
