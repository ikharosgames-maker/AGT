using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views;

public class ControlTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? TextAreaTemplate { get; set; }
    public DataTemplate? ComboTemplate { get; set; }
    public DataTemplate? CheckTemplate { get; set; }
    public DataTemplate? DateTemplate { get; set; }
    public DataTemplate? LabelTemplate { get; set; }
    public DataTemplate? ListViewTemplate { get; set; }
    public DataTemplate? DataGridTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var c = (BlockControl)item;
        return c.ControlType switch
        {
            ControlType.Text => TextTemplate!,
            ControlType.TextArea => TextAreaTemplate!,
            ControlType.Combo => ComboTemplate!,
            ControlType.Check => CheckTemplate!,
            ControlType.Date => DateTemplate!,
            ControlType.Label => LabelTemplate!,
            ControlType.ListView => ListViewTemplate!,
            ControlType.DataGrid => DataGridTemplate!,
            _ => TextTemplate!
        };
    }
}

/* Konvertory (stejné jako dřív) */
public class BooleanToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isSel = value is bool b && b;
        var thickness = parameter is string s && double.TryParse(s, out var t) ? t : 2.0;
        return isSel ? new Thickness(thickness) : new Thickness(0);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class InvertBool : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
