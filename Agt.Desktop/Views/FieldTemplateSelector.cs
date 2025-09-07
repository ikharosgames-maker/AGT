using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public class FieldTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TplLabel { get; set; }
        public DataTemplate? TplTextBox { get; set; }
        public DataTemplate? TplTextArea { get; set; }
        public DataTemplate? TplComboBox { get; set; }
        public DataTemplate? TplCheckBox { get; set; }
        public DataTemplate? TplDate { get; set; }
        public DataTemplate? TplNumber { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not FieldComponentBase f) return base.SelectTemplate(item, container);
            return f.TypeKey switch
            {
                "label" => TplLabel ?? base.SelectTemplate(item, container),
                "textbox" => TplTextBox ?? base.SelectTemplate(item, container),
                "textarea" => TplTextArea ?? base.SelectTemplate(item, container),
                "combobox" => TplComboBox ?? base.SelectTemplate(item, container),
                "checkbox" => TplCheckBox ?? base.SelectTemplate(item, container),
                "date" => TplDate ?? base.SelectTemplate(item, container),
                "number" => TplNumber ?? base.SelectTemplate(item, container),
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
