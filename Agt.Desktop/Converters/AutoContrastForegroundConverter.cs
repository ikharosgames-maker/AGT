using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    public class AutoContrastForegroundConverter : IValueConverter
    {
        private static double Luminance(Color c)
        {
            double srgb(double u) => u <= 0.03928 ? u / 12.92 : Math.Pow((u + 0.055) / 1.055, 2.4);
            var r = srgb(c.R / 255.0);
            var g = srgb(c.G / 255.0);
            var b = srgb(c.B / 255.0);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // očekává Brush (Background). Pokud není, vrátíme bílou.
            if (value is SolidColorBrush scb)
            {
                var L = Luminance(scb.Color);
                return L > 0.5 ? Brushes.Black : Brushes.White;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
