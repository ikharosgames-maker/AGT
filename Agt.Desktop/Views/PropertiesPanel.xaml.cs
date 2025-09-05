using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Agt.Desktop.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public PropertiesPanel() => InitializeComponent();
    }

    public class IntEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value?.ToString() == parameter?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class IntGreaterOrEqualToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!int.TryParse(value?.ToString(), out var v)) return Visibility.Collapsed;
            if (!int.TryParse(parameter?.ToString(), out var p)) p = 0;
            return v >= p ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
