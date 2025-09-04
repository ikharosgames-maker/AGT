using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views;

public class FieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? NumberTemplate { get; set; }
    public DataTemplate? DateTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var fi = (FieldInstance)item;
        return fi.Field.Type switch
        {
            "text" => TextTemplate!,
            "number" => NumberTemplate!,
            "date" => DateTemplate!,
            _ => TextTemplate!
        };
    }
}
