using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        // parameter: "n"  (viditelné pokud Count == n)
        // parameter: "Invert:n" (viditelné pokud Count != n)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (value is int i) ? i : 0;
            var s = parameter?.ToString() ?? "0";
            var invert = false;
            if (s.StartsWith("Invert:", StringComparison.OrdinalIgnoreCase))
            {
                invert = true; s = s.Substring("Invert:".Length);
            }
            int n; int.TryParse(s, out n);
            var show = (v == n);
            if (invert) show = !show;
            return show ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
