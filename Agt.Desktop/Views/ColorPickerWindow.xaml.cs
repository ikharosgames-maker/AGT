using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Agt.Desktop.Views;

public partial class ColorPickerWindow : Window
{
    public double Hue { get; private set; }          // 0..360
    public double Saturation { get; private set; }   // 0..1
    public double Lightness { get; private set; }    // 0..1

    public string SelectedHex { get; private set; } = "#FFFFFF";

    bool _updating = false;

    public ColorPickerWindow(double hue = 0, double saturation = 1, double lightness = 0.5)
    {
        InitializeComponent();
        Hue = hue; Saturation = saturation; Lightness = lightness;
        ApplyToUI();
        RefreshPreview();
    }

    private void ApplyToUI()
    {
        _updating = true;
        HueSlider.Value = Hue;
        SatSlider.Value = Saturation;
        LitSlider.Value = Lightness;

        HueBox.Text = ((int)Hue).ToString(CultureInfo.InvariantCulture);
        SatBox.Text = Saturation.ToString(CultureInfo.InvariantCulture);
        LitBox.Text = Lightness.ToString(CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void RefreshPreview()
    {
        var c = FromHsl(Hue, Saturation, Lightness);
        Preview.Background = new SolidColorBrush(c);
        SelectedHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        HexBox.Text = SelectedHex;
    }

    private static Color FromHsl(double h, double s, double l)
    {
        h = (h % 360 + 360) % 360;
        double c = (1 - System.Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - System.Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;

        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte R = (byte)System.Math.Round((r + m) * 255);
        byte G = (byte)System.Math.Round((g + m) * 255);
        byte B = (byte)System.Math.Round((b + m) * 255);
        return Color.FromRgb(R, G, B);
    }

    /* --- UI handlers --- */
    private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        Hue = e.NewValue; ApplyToUI(); RefreshPreview();
    }
    private void SatSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        Saturation = e.NewValue; ApplyToUI(); RefreshPreview();
    }
    private void LitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating) return;
        Lightness = e.NewValue; ApplyToUI(); RefreshPreview();
    }

    private void HueBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updating) return;
        if (double.TryParse(HueBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            Hue = System.Math.Max(0, System.Math.Min(360, v)); ApplyToUI(); RefreshPreview();
        }
    }
    private void SatBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updating) return;
        if (double.TryParse(SatBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            Saturation = System.Math.Max(0, System.Math.Min(1, v)); ApplyToUI(); RefreshPreview();
        }
    }
    private void LitBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updating) return;
        if (double.TryParse(LitBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            Lightness = System.Math.Max(0, System.Math.Min(1, v)); ApplyToUI(); RefreshPreview();
        }
    }

    private void CopyHex_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(SelectedHex);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
