// Agt.Desktop/Views/FieldTemplateSelector.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Agt.Desktop.Models;     // RenderMode

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Selektor šablon bez potřeby bidingu na Owner.
    /// Z <paramref name="container"/> si sám najde FormCanvas ve vizuálním stromu,
    /// přečte jeho Mode a hledá šablony ve Resources (FormCanvas -> App).
    /// Klíč šablony: "{ModePrefix}_{TypeName}" (např. "Edit_TextBoxField").
    /// </summary>
    public sealed class FieldTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item == null || container == null)
                return base.SelectTemplate(item, container);

            var canvas = FindAncestorFormCanvas(container);
            var modePrefix = canvas?.Mode switch
            {
                RenderMode.Edit => "Edit",
                RenderMode.ReadOnly => "RO",
                RenderMode.Run => "Run",
                _ => "Edit"
            };

            var typeName = item.GetType().Name; // např. "TextBoxField"
            var key = $"{modePrefix}_{typeName}";

            // 1) zkusit najít šablonu ve FormCanvas.Resources
            var dt = canvas?.Resources[key] as DataTemplate;
            if (dt != null) return dt;

            // 2) fallback: App resources
            dt = Agt.Desktop.App.Current?.TryFindResource(key) as DataTemplate;
            if (dt != null) return dt;

            // 3) poslední fallback: zkusit Edit_ pro daný typ
            dt = (canvas?.Resources[$"Edit_{typeName}"] as DataTemplate)
                 ?? (Agt.Desktop.App.Current?.TryFindResource($"Edit_{typeName}") as DataTemplate);

            return dt ?? base.SelectTemplate(item, container);
        }

        private static Controls.FormCanvas? FindAncestorFormCanvas(DependencyObject d)
        {
            while (d != null)
            {
                if (d is Controls.FormCanvas fc) return fc;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
