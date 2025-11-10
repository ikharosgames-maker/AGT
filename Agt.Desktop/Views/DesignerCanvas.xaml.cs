using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas : UserControl
    {
        private DesignerViewModel? VM => DataContext as DesignerViewModel;

        private UIElement? _dragElement;
        private Point _dragStart;
        private double _startX, _startY;

        public DesignerCanvas()
        {
            InitializeComponent();
            Loaded += (s, e) => Redraw();
            SizeChanged += (s, e) => Redraw();
        }

        // ========== PUBLIC API ==========

        public void Redraw()
        {
            if (CanvasHost == null || VM == null) return;
            CanvasHost.Children.Clear();

            // Grid overlay (lehké čáry)
            if (VM.ShowGrid && VM.GridSize > 1)
            {
                var g = VM.GridSize;
                var w = Math.Max(ActualWidth, 1200);
                var h = Math.Max(ActualHeight, 800);

                for (int x = 0; x < w; x += g)
                {
                    CanvasHost.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = x,
                        X2 = x,
                        Y1 = 0,
                        Y2 = h,
                        Stroke = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)),
                        StrokeThickness = 1
                    });
                }
                for (int y = 0; y < h; y += g)
                {
                    CanvasHost.Children.Add(new System.Windows.Shapes.Line
                    {
                        X1 = 0,
                        X2 = w,
                        Y1 = y,
                        Y2 = y,
                        Stroke = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)),
                        StrokeThickness = 1
                    });
                }
            }

            // Items
            foreach (var it in VM.Items)
            {
                var border = new Border
                {
                    Background = Brushes.AliceBlue,
                    BorderBrush = Brushes.SteelBlue,
                    BorderThickness = new Thickness(1.25),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8),
                    Width = it.Width,
                    Height = it.Height,
                    Tag = it
                };

                var tb = new TextBlock { Text = it.Title, FontWeight = FontWeights.SemiBold };
                border.Child = tb;

                CanvasHost.Children.Add(border);
                Canvas.SetLeft(border, it.X);
                Canvas.SetTop(border, it.Y);

                // Dragging
                border.PreviewMouseLeftButtonDown += Item_PreviewMouseLeftButtonDown;
            }
        }

        // ========== EVENT HANDLERY – Canvas (hlášky požadovaly tyto názvy) ==========

        private void Canvas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Prozatím žádné context menu – potlačíme
            e.Handled = true;
        }

        private void Canvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _dragElement?.ReleaseMouseCapture();
            _dragElement = null;
            ShouldSuspendAnchorDock = false;
        }

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            // Ukončit drag, když myš opustí plátno
            if (_dragElement != null)
            {
                _dragElement.ReleaseMouseCapture();
                _dragElement = null;
                ShouldSuspendAnchorDock = false;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // klik do prázdna – zrušíme výběr
            FocusManager.SetFocusedElement(this, this);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragElement != null)
            {
                _dragElement.ReleaseMouseCapture();
                _dragElement = null;
                ShouldSuspendAnchorDock = false;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragElement == null || e.LeftButton != MouseButtonState.Pressed || VM == null) return;

            var p = e.GetPosition(CanvasHost);
            var dx = p.X - _dragStart.X;
            var dy = p.Y - _dragStart.Y;

            if ((_dragElement as FrameworkElement)?.Tag is DesignerViewModel.DesignItem it)
            {
                var nx = VM.SnapToGrid(_startX + dx);
                var ny = VM.SnapToGrid(_startY + dy);
                it.X = nx; it.Y = ny;
                Canvas.SetLeft(_dragElement, nx);
                Canvas.SetTop(_dragElement, ny);
            }
        }

        private void RootCanvas_DragOver(object sender, DragEventArgs e)
        {
            // Povolit drop všeho, co budeme případně umět zpracovat v budoucnu
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void RootCanvas_Drop(object sender, DragEventArgs e)
        {
            // Zatím bez speciální logiky (můžeme doplnit: soubor JSON → přidat blok)
            e.Handled = true;
        }

        // ========== EVENT HANDLERY – prvky (border) ==========

        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (VM == null) return;

            _dragElement = (UIElement)sender;
            _dragStart = e.GetPosition(CanvasHost);

            if ((_dragElement as FrameworkElement)?.Tag is DesignerViewModel.DesignItem it)
            {
                _startX = it.X;
                _startY = it.Y;
            }

            ShouldSuspendAnchorDock = true; // během drag potlačíme dokování kotev
            _dragElement.CaptureMouse();
            e.Handled = true;
        }

        // ========== THUMB HANDLERY – pokud je v XAML používáš, ať to kompiluje ==========

        private void MoveThumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            ShouldSuspendAnchorDock = true;
        }

        private void MoveThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // Placeholder: vlastní logika posunu přes Thumb lze doplnit později
        }

        private void MoveThumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            ShouldSuspendAnchorDock = false;
        }

        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            // Placeholder: změna velikosti – doplníme až budeme mít rohy/úchyty
        }
    }
}
