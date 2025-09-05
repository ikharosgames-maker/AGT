using System;
using System.Globalization;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    public class CountGreaterOrEqualConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int i ? i : 0;
            var threshold = 1;
            if (parameter != null && int.TryParse(parameter.ToString(), out var t)) threshold = t;
            return count >= threshold;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
