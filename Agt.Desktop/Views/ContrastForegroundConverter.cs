using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public sealed class ContrastForegroundConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush b)
            {
                var c = b.Color;
                double L = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
                return L > 0.6 ? Brushes.Black : Brushes.White;
            }
            return SystemColors.ControlText;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
