using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // ===== Jednotlivé položky =====

        private void PickForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushWithPalette(sender as FrameworkElement, "Foreground");

        private void PickBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushWithPalette(sender as FrameworkElement, "Background");

        // ===== Hromadné (bulk) =====

        private void PickBulkForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushWithPalette(sender as FrameworkElement, "BulkForeground");

        private void PickBulkBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushWithPalette(sender as FrameworkElement, "BulkBackground");

        // ===== Společná logika výběru barvy přes malou paletu =====

        private void PickBrushWithPalette(FrameworkElement? origin, string propertyName)
        {
            if (origin == null) return;

            // Předpřipravená paleta (možné rozšířit)
            var colors = new[]
            {
                Colors.Black, Colors.White, Colors.DimGray, Colors.Silver,
                Colors.Red, Colors.Orange, Colors.Gold, Colors.Yellow,
                Colors.LimeGreen, Colors.SeaGreen, Colors.Teal, Colors.SteelBlue,
                Colors.DodgerBlue, Colors.MediumPurple, Colors.DeepPink, Colors.Brown,
                Color.FromRgb(0x1E,0x1E,0x1E), Color.FromRgb(0x2D,0x2D,0x30), // VS-like dark
                Color.FromRgb(0xF3,0xF3,0xF3), Color.FromRgb(0xEE,0xEE,0xEE)  // světlé
            };

            var popup = new Popup
            {
                StaysOpen = true,
                PlacementTarget = origin,
                Placement = PlacementMode.Bottom,
                AllowsTransparency = true,
                Child = BuildPaletteGrid(colors, brush =>
                {
                    SetBrushProperty(origin.DataContext, propertyName, brush);
                })
            };

            // zavírání mimo
            popup.Opened += (_, __) =>
            {
                Agt.Desktop.App.Current.MainWindow.PreviewMouseDown += CloseOnOutsideClick;
            };
            void CloseOnOutsideClick(object? s, System.Windows.Input.MouseButtonEventArgs e)
            {
                if (popup.IsOpen && !IsAncestorOf(popup.Child, e.OriginalSource as DependencyObject))
                {
                    popup.IsOpen = false;
                    Agt.Desktop.App.Current.MainWindow.PreviewMouseDown -= CloseOnOutsideClick;
                }
            }

            popup.IsOpen = true;
        }

        private UIElement BuildPaletteGrid(Color[] colors, Action<Brush> onPick)
        {
            var grid = new UniformGrid
            {
                Rows = (int)Math.Ceiling(colors.Length / 6.0),
                Columns = 6,
                Margin = new Thickness(6),
            };

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = grid
            };

            foreach (var c in colors)
            {
                var btn = new Button
                {
                    Margin = new Thickness(4),
                    Padding = new Thickness(0),
                    Width = 24,
                    Height = 24,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(c),
                    ToolTip = $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                };
                btn.Click += (_, __) =>
                {
                    onPick(btn.Background);
                    // zavři popup (nejbližší Popup předka)
                    var p = FindAncestor<Popup>(btn);
                    if (p != null) p.IsOpen = false;
                };
                grid.Children.Add(btn);
            }

            // zabalíme do drobného „hostu“, aby Popup.Child byl právě jeden element
            var host = new Grid();
            host.Children.Add(border);
            return host;
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        private static bool IsAncestorOf(DependencyObject? root, DependencyObject? node)
        {
            while (node != null)
            {
                if (node == root) return true;
                node = VisualTreeHelper.GetParent(node);
            }
            return false;
        }

        private static void SetBrushProperty(object? ctx, string propName, Brush value)
        {
            if (ctx == null) return;
            var prop = ctx.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite && typeof(Brush).IsAssignableFrom(prop.PropertyType))
            {
                prop.SetValue(ctx, value);
                return;
            }

            // fallback: pokud je to string (např. HEX v nějaké implementaci)
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
            {
                if (value is SolidColorBrush scb)
                {
                    string hex = $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
                    prop.SetValue(ctx, hex);
                }
            }
        }
    }
}
