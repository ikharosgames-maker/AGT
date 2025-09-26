using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Viditelné, pokud se vstupní string rovná ConverterParameter (case-insensitive); jinak Collapsed.
    /// </summary>
    public sealed class StringEqualsToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString() ?? string.Empty;
            var p = parameter?.ToString() ?? string.Empty;

            bool equals = string.Equals(s, p, StringComparison.OrdinalIgnoreCase);
            if (Invert) equals = !equals;

            return equals ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
