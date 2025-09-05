using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int i ? i : 0;
            var param = parameter?.ToString() ?? "1";
            var invert = false;

            if (param.Contains(":"))
            {
                var parts = param.Split(':');
                invert = parts[0].Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                         parts[0].Equals("Less", StringComparison.OrdinalIgnoreCase);
                if (int.TryParse(parts[^1], out var thr2))
                    return ToVis(invert ? count < thr2 : count >= thr2);
                return ToVis(invert ? count < 1 : count >= 1);
            }

            if (int.TryParse(param, out var thr))
                return ToVis(count >= thr);

            return ToVis(count >= 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static Visibility ToVis(bool v) => v ? Visibility.Visible : Visibility.Collapsed;
    }
}
