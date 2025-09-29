using System;
using System.Globalization;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    public sealed class ReferenceEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => ReferenceEquals(value, parameter);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
