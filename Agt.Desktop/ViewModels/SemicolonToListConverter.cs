using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Agt.Desktop.ViewModels
{
    public sealed class SemicolonToListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value?.ToString() ?? "";
            var arr = s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(p => p.Trim())
                       .ToArray();
            return arr;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
