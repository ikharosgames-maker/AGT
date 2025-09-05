using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;
using Agt.Desktop.Views.Adorners;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas : UserControl
    {
        private SelectionService _selection => (SelectionService)Application.Current.Resources["SelectionService"];
        private SelectionToolbarAdorner? _toolbarAdorner;

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

        // --- Grid background (programově, kvůli bindingu na GridSize) ---
        private void UpdateGridBackground()
        {
            if (VM == null || ItemsHost == null) return;

            if (!VM.ShowGrid)
            {
                (ItemsHost.ItemsPanelRoot as Panel)!.Background = Brushes.Transparent;
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

            (ItemsHost.ItemsPanelRoot as Panel)!.Background = brush;
        }

        // --- Drag from library ---
        private void RootCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("field/key")) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void RootCanvas_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("field/key")) return;
            if (VM == null) return;

            var key = (string)e.Data.GetData("field/key")!;
            var pos = e.GetPosition((IInputElement)sender);
            VM.CreateFromLibrary(key, pos);
            e.Handled = true;
            UpdateFloatingBar();
        }

        // --- Výběr ---
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
            UpdateFloatingBar();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender)
            {
                _selection.Clear();
                UpdateFloatingBar();
            }
        }

        private void Canvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // pravý klik nemění výběr
        }

        // --- Floating bar nad výběrem ---
        private void UpdateFloatingBar()
        {
            var layer = AdornerLayer.GetAdornerLayer(this);
            if (layer == null) return;

            if (_selection.Count == 0)
            {
                if (_toolbarAdorner != null) { layer.Remove(_toolbarAdorner); _toolbarAdorner = null; }
                return;
            }

            var rect = GetSelectionBounds();
            if (_toolbarAdorner == null)
            {
                _toolbarAdorner = new SelectionToolbarAdorner(this, DataContext!);
                layer.Add(_toolbarAdorner);
            }
            _toolbarAdorner.UpdateBounds(rect);
        }

        private Rect GetSelectionBounds()
        {
            var items = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (items.Count == 0) return new Rect(0, 0, 0, 0);

            double minX = items.Min(i => i.X);
            double minY = items.Min(i => i.Y);
            double maxX = items.Max(i => i.X + i.Width);
            double maxY = items.Max(i => i.Y + i.Height);

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }

        // --- Move / Resize (Thumb handlers) ---
        private Point _dragStart;
        private double _origX, _origY, _origW, _origH;

        private void MoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            _dragStart = Mouse.GetPosition(ItemsHost.ItemsPanelRoot);
            _origX = f.X; _origY = f.Y;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not FieldComponentBase f) return;
            if (VM == null) return;

            var cur = Mouse.GetPosition(ItemsHost.ItemsPanelRoot);
            var dx = cur.X - _dragStart.X;
            var dy = cur.Y - _dragStart.Y;

            double nx = _origX + dx;
            double ny = _origY + dy;

            if (VM.SnapToGrid && VM.GridSize > 0)
            {
                nx = Math.Round(nx / VM.GridSize) * VM.GridSize;
                ny = Math.Round(ny / VM.GridSize) * VM.GridSize;
            }

            f.X = nx; f.Y = ny;
        }

        private void MoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            // nic – ponecháno pro budoucí undo/redo
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

            f.Width = dw;
            f.Height = dh;
        }

        // --- Pomocný converter v menu (porovnání GridSize) ---
        public class EqualToConverter : System.Windows.Data.IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value == null || parameter == null) return false;
                return value.ToString() == parameter.ToString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
