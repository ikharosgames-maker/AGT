using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas : UserControl
    {
        private SelectionService Selection => (SelectionService)Agt.Desktop.App.Current.Resources["SelectionService"];
        private DesignerViewModel? VM => DataContext as DesignerViewModel;

        public DesignerCanvas()
        {
            InitializeComponent();

            // původní handlery
            Loaded += DesignerCanvas_Loaded;
            Unloaded += DesignerCanvas_Unloaded;
            SizeChanged += (_, __) => UpdateGridBackground();

            // háky na partial Anchor/Dock (metody jsou v DesignerCanvas.AnchorDock.cs)
            Loaded += AnchorDock_OnLoaded;
            Unloaded += AnchorDock_OnUnloaded;
            SizeChanged += AnchorDock_OnSizeChanged;
        }

        private void DesignerCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.PropertyChanged += VM_PropertyChanged;
            UpdateGridBackground();
        }

        private void DesignerCanvas_Unloaded(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.PropertyChanged -= VM_PropertyChanged;
            ClearLasso();
        }

        private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DesignerViewModel.ShowGrid) or nameof(DesignerViewModel.GridSize))
                UpdateGridBackground();
        }

        // ===== Items panel =====
        private Panel? GetItemsPanel()
        {
            ItemsHost.ApplyTemplate();
            var presenter = FindVisualChild<ItemsPresenter>(ItemsHost);
            presenter ??= ItemsHost.Template.FindName("ItemsPresenter", ItemsHost) as ItemsPresenter ?? new ItemsPresenter();
            presenter.ApplyTemplate();
            return VisualTreeHelper.GetChild(presenter, 0) as Panel;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var sub = FindVisualChild<T>(child);
                if (sub != null) return sub;
            }
            return null;
        }

        // ===== Grid background =====
        private void UpdateGridBackground()
        {
            var panel = GetItemsPanel();
            if (panel == null) return;

            var baseBrush = Agt.Desktop.App.Current.Resources["CanvasBackgroundBrush"] as SolidColorBrush ?? Brushes.Gray;
            if (VM == null || !VM.ShowGrid)
            {
                panel.Background = baseBrush;
                return;
            }

            int size = Math.Max(2, (int)VM.GridSize);
            var dot = Agt.Desktop.App.Current.Resources["GridDotBrush"] as SolidColorBrush ?? new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));

            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing
            {
                Brush = new SolidColorBrush(baseBrush.Color),
                Geometry = new RectangleGeometry(new Rect(0, 0, size, size))
            });
            group.Children.Add(new GeometryDrawing
            {
                Brush = dot,
                Geometry = new RectangleGeometry(new Rect(0, 0, 1, 1))
            });

            var brush = new DrawingBrush(group)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, size, size),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            panel.Background = brush;
        }

        // ===== DnD z knihovny =====
        private void RootCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("field/key") ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void RootCanvas_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("field/key") || VM == null) return;
            var panel = GetItemsPanel(); if (panel == null) return;

            var key = (string)e.Data.GetData("field/key")!;
            var pos = e.GetPosition(panel);
            VM.CreateFromLibrary(key, pos);
            e.Handled = true;
        }

        // ===== Klik/CTRL-klik (neblokovat Thumb) =====
        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Thumb) return; // drag přes Thumb má prioritu

            var fe = sender as FrameworkElement;
            var item = fe?.DataContext;
            if (item == null) return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                Selection.Toggle(item);
            else
                Selection.SelectSingle(item);
        }

        // ===== Lasso =====
        private Point? _lassoStart;
        private readonly RectangleGeometry _lassoRectGeom = new();
        private UIElement? _lassoCapturedOn;

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var panel = GetItemsPanel(); if (panel == null) return;
            if (e.Source != sender) return; // jen klik do „prázdna“

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                Selection.Clear();

            _lassoStart = e.GetPosition(panel);

            var path = new System.Windows.Shapes.Path
            {
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255)),
                Data = _lassoRectGeom,
                IsHitTestVisible = false
            };
            LassoLayer.Children.Clear();
            LassoLayer.Children.Add(path);

            _lassoCapturedOn = sender as UIElement;
            _lassoCapturedOn?.CaptureMouse();
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_lassoStart == null) return;
            var panel = GetItemsPanel(); if (panel == null) return;

            var cur = e.GetPosition(panel);
            var x = Math.Min(_lassoStart.Value.X, cur.X);
            var y = Math.Min(_lassoStart.Value.Y, cur.Y);
            var w = Math.Abs(cur.X - _lassoStart.Value.X);
            var h = Math.Abs(cur.Y - _lassoStart.Value.Y);
            _lassoRectGeom.Rect = new Rect(x, y, w, h);

            var target = VM!.Items
                .Where(it => _lassoRectGeom.Rect.Contains(new Rect(it.X, it.Y, it.Width, it.Height)))
                .Cast<object>()
                .ToHashSet();

            Selection.ReplaceWith(target);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) => ClearLasso();
        private void Canvas_MouseLeave(object sender, MouseEventArgs e) => ClearLasso();
        private void Canvas_LostMouseCapture(object sender, MouseEventArgs e) => ClearLasso();

        private void ClearLasso()
        {
            _lassoStart = null;
            _lassoRectGeom.Rect = Rect.Empty;
            LassoLayer.Children.Clear();
            _lassoCapturedOn?.ReleaseMouseCapture();
            _lassoCapturedOn = null;
        }

        // ===== Move/Resize =====
        private Point _dragStart;
        private double _origX, _origY;

        private bool _suspendAnchorDock;
        private bool ShouldSuspendAnchorDock() => _suspendAnchorDock; // čte partial AnchorDock

        private void MoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            _dragStart = Mouse.GetPosition(this);
            _origX = f.X; _origY = f.Y;

            _suspendAnchorDock = true; // během drag neaplikovat Anchor/Dock
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            if (VM == null) return;

            var cur = Mouse.GetPosition(this);
            var dx = cur.X - _dragStart.X;
            var dy = cur.Y - _dragStart.Y;

            double nx = _origX + dx;
            double ny = _origY + dy;

            if (VM.SnapToGrid && VM.GridSize > 0)
            {
                nx = Math.Round(nx / VM.GridSize) * VM.GridSize;
                ny = Math.Round(ny / VM.GridSize) * VM.GridSize;
            }

            var host = GetItemsPanel();
            double boundW = host?.ActualWidth ?? ActualWidth;
            double boundH = host?.ActualHeight ?? ActualHeight;

            nx = Math.Max(0, Math.Min(nx, Math.Max(0, boundW - f.Width)));
            ny = Math.Max(0, Math.Min(ny, Math.Max(0, boundH - f.Height)));

            f.X = nx; f.Y = ny;
        }

        private void MoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _suspendAnchorDock = false;

            if ((sender as FrameworkElement)?.DataContext is FieldComponentBase f)
            {
                var parent = GetItemsPanel();
                var size = new Size(parent?.ActualWidth ?? ActualWidth, parent?.ActualHeight ?? ActualHeight);
                AnchorDockService.ResetBaseline(f, size);
                AnchorDockService.Apply(f, size);
            }

            if (IsMouseCaptured) ReleaseMouseCapture();
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            if (VM == null) return;

            var dw = Math.Max(10, f.Width + e.HorizontalChange);
            var dh = Math.Max(10, f.Height + e.VerticalChange);

            if (VM.SnapToGrid && VM.GridSize > 0)
            {
                dw = Math.Max(10, Math.Round(dw / VM.GridSize) * VM.GridSize);
                dh = Math.Max(10, Math.Round(dh / VM.GridSize) * VM.GridSize);
            }

            var host = GetItemsPanel();
            double boundW = host?.ActualWidth ?? ActualWidth;
            double boundH = host?.ActualHeight ?? ActualHeight;

            dw = Math.Min(dw, Math.Max(10, boundW - f.X));
            dh = Math.Min(dh, Math.Max(10, boundH - f.Y));

            f.Width = dw;
            f.Height = dh;

            // udrž bounds + anchor/dock konzistenci
            var size = new Size(boundW, boundH);
            AnchorDockService.Apply(f, size);
        }

        private void Canvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // TODO: context menu (Smazat/Duplikovat/…)
        }
    }
}
