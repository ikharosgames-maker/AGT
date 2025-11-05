using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    /// Vrátí UnsetValue, když je vstup null -> použije se default ze stylu
    public sealed class NullToUnsetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value ?? DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
