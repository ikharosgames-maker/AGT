using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public partial class ColorPickerWindow : Window
    {
        // Veřejné API kompatibilní s projektem:
        // - Konstruktor ColorPickerWindow(SolidColorBrush?)
        // - Výsledek přes ResultBrush (SolidColorBrush?)
        public SolidColorBrush? ResultBrush { get; private set; }

        // Stav (HSV)
        private double _h; // 0..360
        private double _s; // 0..1
        private double _v; // 0..1

        private bool _draggingSv;
        private bool _updatingUi;  // guard proti reentranci

        // Transform pro marker (funguje i v Gridu)
        private readonly TranslateTransform _svMarkerTransform = new();

        public ColorPickerWindow() : this(null) { }

        public ColorPickerWindow(SolidColorBrush? initial)
        {
            InitializeComponent();

            // Marker posouváme přes RenderTransform (žádný Canvas.SetLeft/Top)
            SvMarker.RenderTransform = _svMarkerTransform;
            Panel.SetZIndex(SvMarker, 10);

            if (initial != null)
            {
                FromColor(initial.Color, out _h, out _s, out _v);
                HexBox.Text = ToHex(initial.Color);
            }
            else
            {
                var c = (Color)ColorConverter.ConvertFromString("#333333")!;
                FromColor(c, out _h, out _s, out _v);
                HexBox.Text = "#333333";
            }

            HueSlider.Value = _h;
            UpdateSvGradient();
            UpdateSvMarker();
            UpdatePreviewFromHsv();
        }

        // ===== UI události =====

        private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingUi) return;
            _h = HueSlider.Value;
            UpdateSvGradient();
            UpdatePreviewFromHsv();
        }

        private void SvCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggingSv = true;
            SvCanvas.CaptureMouse();
            SetSvFromPoint(e.GetPosition(SvCanvas));
        }

        private void SvCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_draggingSv) return;
            SetSvFromPoint(e.GetPosition(SvCanvas));
        }

        private void SvCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_draggingSv) return;
            _draggingSv = false;
            SvCanvas.ReleaseMouseCapture();
        }

        private void SvCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSvMarker(); // přepočítat pozici markeru po změně rozměrů
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_updatingUi) return;

            if (TryParseHex(HexBox.Text, out var c))
            {
                _updatingUi = true;
                FromColor(c, out _h, out _s, out _v);
                HueSlider.Value = _h;        // vyvolá ValueChanged, ale guard drží
                UpdateSvGradient();
                UpdateSvMarker();
                UpdatePreview(c);
                _updatingUi = false;
            }
        }

        private void NormalizeHex_Click(object sender, RoutedEventArgs e)
        {
            if (TryParseHex(HexBox.Text, out var c))
            {
                _updatingUi = true;
                HexBox.Text = ToHex(c);
                _updatingUi = false;
            }
        }

        private void Palette_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Background is SolidColorBrush sb)
            {
                _updatingUi = true;
                FromColor(sb.Color, out _h, out _s, out _v);
                HueSlider.Value = _h;
                HexBox.Text = ToHex(sb.Color);
                UpdateSvGradient();
                UpdateSvMarker();
                UpdatePreview(sb.Color);
                _updatingUi = false;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var c = ColorFromHSV(_h, _s, _v);
            ResultBrush = new SolidColorBrush(c);
            DialogResult = true;
            Close();
        }

        // ===== HSV/Color pomocné metody =====

        private void SetSvFromPoint(Point p)
        {
            var w = SvCanvas.ActualWidth;
            var h = SvCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            _s = Math.Clamp(p.X / w, 0, 1);
            _v = Math.Clamp(1.0 - (p.Y / h), 0, 1);

            UpdateSvMarker();
            UpdatePreviewFromHsv();
        }

        private void UpdateSvMarker()
        {
            var w = SvCanvas.ActualWidth;
            var h = SvCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var x = _s * w;
            var y = (1.0 - _v) * h;

            // Posun markeru pomocí RenderTransform – funguje i v Gridu
            _svMarkerTransform.X = x - (SvMarker.Width / 2.0);
            _svMarkerTransform.Y = y - (SvMarker.Height / 2.0);
        }

        private void UpdateSvGradient()
        {
            var hueColor = ColorFromHSV(_h, 1.0, 1.0);
            SvHueColorStop.Color = hueColor;
        }

        private void UpdatePreviewFromHsv()
        {
            var c = ColorFromHSV(_h, _s, _v);
            UpdatePreview(c);

            // zapiš normalizovaný HEX (bez rekurze)
            _updatingUi = true;
            HexBox.Text = ToHex(c);
            _updatingUi = false;
        }

        private void UpdatePreview(Color c)
        {
            PreviewSwatch.Background = new SolidColorBrush(c);
        }

        // HSV -> Color
        private static Color ColorFromHSV(double h, double s, double v)
        {
            h = (h % 360 + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);

            if (s <= 0)
            {
                byte gv = (byte)Math.Round(v * 255);
                return Color.FromRgb(gv, gv, gv);
            }

            double c = v * s;
            double x = c * (1 - Math.Abs(((h / 60) % 2) - 1));
            double m = v - c;

            (double r1, double g1, double b1) = (0, 0, 0);
            if (h < 60) (r1, g1, b1) = (c, x, 0);
            else if (h < 120) (r1, g1, b1) = (x, c, 0);
            else if (h < 180) (r1, g1, b1) = (0, c, x);
            else if (h < 240) (r1, g1, b1) = (0, x, c);
            else if (h < 300) (r1, g1, b1) = (x, 0, c);
            else (r1, g1, b1) = (c, 0, x);

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return Color.FromRgb(r, g, b);
        }

        // Color -> HSV
        private static void FromColor(Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            // Hue
            if (delta == 0) h = 0;
            else if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - g) / delta) + 4);
            if (h < 0) h += 360;

            // Saturation
            s = (max == 0) ? 0 : delta / max;

            // Value
            v = max;
        }

        // HEX utils
        private static bool TryParseHex(string? s, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            try
            {
                if (!s.StartsWith("#", StringComparison.Ordinal)) s = "#" + s;
                var c = (Color)ColorConverter.ConvertFromString(s)!;
                color = c;
                return true;
            }
            catch { return false; }
        }

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
