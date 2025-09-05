using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas : UserControl
    {
        private SelectionService _selection => (SelectionService)Application.Current.Resources["SelectionService"];
        private DesignerViewModel? VM => DataContext as DesignerViewModel;

        public DesignerCanvas()
        {
            InitializeComponent();
            Loaded += DesignerCanvas_Loaded;
            Unloaded += DesignerCanvas_Unloaded;
        }

        private void DesignerCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.PropertyChanged += VM_PropertyChanged;
            UpdateGridBackground();
        }
        private void DesignerCanvas_Unloaded(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.PropertyChanged -= VM_PropertyChanged;
        }
        private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DesignerViewModel.ShowGrid) or nameof(DesignerViewModel.GridSize))
                UpdateGridBackground();
        }

        // Pomocné – získá Canvas panel
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

        // Mřížka
        private void UpdateGridBackground()
        {
            if (VM == null) return;
            var panel = GetItemsPanel();
            if (panel == null) return;

            if (!VM.ShowGrid)
            {
                panel.Background = Brushes.Transparent;
                return;
            }

            var size = Math.Max(2, VM.GridSize);
            var brush = new DrawingBrush(new GeometryDrawing
            {
                Geometry = new GeometryGroup
                {
                    Children = new GeometryCollection
                    {
                        new RectangleGeometry(new Rect(0, 0, size, size)),
                        new RectangleGeometry(new Rect(0,0,1,1))
                    }
                },
                Pen = null,
                Brush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255))
            })
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, size, size),
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.None
            };

            panel.Background = brush;
        }

        // Drag&Drop z knihovny
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

        // Výběr klikem (Ctrl = toggle)
        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var item = fe?.DataContext;
            if (item == null) return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                _selection.Toggle(item);
            else
                _selection.SelectSingle(item);

            e.Handled = true;
        }

        // Klik do prázdna = zrušit výběr nebo začít lasso
        private Point? _lassoStart;
        private RectangleGeometry _lassoRectGeom = new();

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var panel = GetItemsPanel(); if (panel == null) return;
            if (e.Source != sender)
                return;

            Keyboard.Focus(this);

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                _selection.Clear();

            _lassoStart = e.GetPosition(panel);

            // vizuální lasso obdélník
            var path = new System.Windows.Shapes.Path
            {
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255)), // poloprůhledné
                Data = _lassoRectGeom,
                IsHitTestVisible = false
            };
            LassoLayer.Children.Clear();
            LassoLayer.Children.Add(path);

            CaptureMouse();
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

            // průběžný výběr (Ctrl = přidávací režim)
            bool additive = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            foreach (var it in (DataContext as DesignerViewModel)!.Items)
            {
                var r = new Rect(it.X, it.Y, it.Width, it.Height);
                bool hit = _lassoRectGeom.Rect.Contains(r);
                if (additive)
                {
                    if (hit && !_selection.IsSelected(it)) _selection.Toggle(it);
                }
                else
                {
                    // pokud není v lasso, odeber z výběru
                    if (!hit && _selection.IsSelected(it)) _selection.Toggle(it);
                    if (hit && !_selection.IsSelected(it)) _selection.Toggle(it);
                }
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_lassoStart != null)
            {
                _lassoStart = null;
                LassoLayer.Children.Clear();
                ReleaseMouseCapture();
            }
        }

        private void Canvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // pravý klik nic nemění
        }

        // Přesun/resize s limitem bounds
        private Point _dragStart;
        private double _origX, _origY, _origW, _origH;

        private void MoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            var panel = GetItemsPanel(); if (panel == null) return;

            _dragStart = Mouse.GetPosition(panel);
            _origX = f.X; _origY = f.Y;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            if (VM == null) return;
            var panel = GetItemsPanel(); if (panel == null) return;

            var cur = Mouse.GetPosition(panel);
            var dx = cur.X - _dragStart.X;
            var dy = cur.Y - _dragStart.Y;

            double nx = _origX + dx;
            double ny = _origY + dy;

            if (VM.SnapToGrid && VM.GridSize > 0)
            {
                nx = Math.Round(nx / VM.GridSize) * VM.GridSize;
                ny = Math.Round(ny / VM.GridSize) * VM.GridSize;
            }

            // bounds
            nx = Math.Max(0, Math.Min(nx, panel.ActualWidth - f.Width));
            ny = Math.Max(0, Math.Min(ny, panel.ActualHeight - f.Height));

            f.X = nx; f.Y = ny;
        }

        private void MoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            if (VM == null) return;
            var panel = GetItemsPanel(); if (panel == null) return;

            _origW = f.Width; _origH = f.Height;

            var dw = Math.Max(10, _origW + e.HorizontalChange);
            var dh = Math.Max(10, _origH + e.VerticalChange);

            if (VM.SnapToGrid && VM.GridSize > 0)
            {
                dw = Math.Max(10, Math.Round(dw / VM.GridSize) * VM.GridSize);
                dh = Math.Max(10, Math.Round(dh / VM.GridSize) * VM.GridSize);
            }

            // bounds – šířka/výška tak, aby nepřesáhla panel
            dw = Math.Min(dw, Math.Max(10, panel.ActualWidth - f.X));
            dh = Math.Min(dh, Math.Max(10, panel.ActualHeight - f.Y));

            f.Width = dw;
            f.Height = dh;
        }
    }
}
