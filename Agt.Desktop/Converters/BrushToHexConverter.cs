using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    public class BrushToHexConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush scb)
            {
                var c = scb.Color;
                return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return string.Empty;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim();
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (!s.StartsWith("#")) s = "#" + s;

            if (s.Length == 7) s = "#FF" + s[1..]; // doplnit A

            if (s.Length == 9 &&
                byte.TryParse(s.Substring(1, 2), NumberStyles.HexNumber, culture, out var a) &&
                byte.TryParse(s.Substring(3, 2), NumberStyles.HexNumber, culture, out var r) &&
                byte.TryParse(s.Substring(5, 2), NumberStyles.HexNumber, culture, out var g) &&
                byte.TryParse(s.Substring(7, 2), NumberStyles.HexNumber, culture, out var b))
            {
                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            return Binding.DoNothing;
        }
    }
}
