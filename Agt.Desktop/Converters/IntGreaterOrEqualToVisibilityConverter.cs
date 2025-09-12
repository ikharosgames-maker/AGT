using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    public sealed class IntGreaterOrEqualToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int val = 0; int.TryParse(value?.ToString(), out val);
            int par = 0; int.TryParse(parameter?.ToString(), out par);
            return val >= par ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
