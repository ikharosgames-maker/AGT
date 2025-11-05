using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    // string ("Segoe UI") <-> FontFamily
    public sealed class FontFamilyNameConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
                return new FontFamily(s);
            return new FontFamily("Segoe UI");
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FontFamily ff)
                return ff.Source;
            return null;
        }
    }
}
