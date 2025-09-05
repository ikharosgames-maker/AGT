using System.Windows;
using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    public class FieldTemplateSelector : DataTemplateSelector
    {
        public string? DefaultKey { get; set; } = "DefaultFieldTemplate";
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement fe && !string.IsNullOrEmpty(DefaultKey))
            {
                var dt = fe.TryFindResource(DefaultKey) as DataTemplate;
                if (dt != null) return dt;
            }
            return base.SelectTemplate(item, container);
        }
    }
}
