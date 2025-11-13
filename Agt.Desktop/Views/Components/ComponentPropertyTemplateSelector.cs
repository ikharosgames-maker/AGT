using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views.Components
{
    public sealed class ComponentPropertyTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return Lookup(container, "Props.Fallback");

            var fe = container as FrameworkElement;
            return item switch
            {
                TextAreaField => Lookup(fe, "Props.TextArea"),
                ComboBoxField => Lookup(fe, "Props.ComboBox"),
                CheckBoxField => Lookup(fe, "Props.CheckBox"),
                _ => Lookup(fe, "Props.Fallback")
            };
        }

        private static DataTemplate Lookup(DependencyObject d, string key)
        {
            if (d is FrameworkElement fe && fe.TryFindResource(key) is DataTemplate dt) return dt;
            // fallback – hledání z AppResources
            return Agt.Desktop.App.Current.TryFindResource(key) as DataTemplate ?? new DataTemplate();
        }
    }
}
