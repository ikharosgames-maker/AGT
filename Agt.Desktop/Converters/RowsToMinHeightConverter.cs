using System;
using System.Globalization;
using System.Windows.Data;

namespace Agt.Desktop.Converters
{
    /// Vypočte MinHeight textové oblasti z [Rows, FontSize]
    public sealed class RowsToMinHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int rows = 1;
            double fontSize = 12.0;

            if (values?.Length > 0 && values[0] is int r && r > 0) rows = r;
            if (values?.Length > 1 && values[1] is double fs && fs > 0) fontSize = fs;

            // přibližná výška řádku ~ 1.4 * fontSize + malé odsazení
            var line = fontSize * 1.4;
            var padding = 8.0;
            return rows * line + padding;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => Array.Empty<object>();
    }
}
