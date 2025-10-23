using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Vybere DataTemplate pro komponentu:
    /// 1) FieldComponentBase -> "Universal_{SkutecnyTyp}"
    /// 2) DTO s TypeKey      -> "Universal_{Map(TypeKey)}"
    /// 3) Fallback           -> "Universal_FieldComponentBase"
    /// Navíc má interní fallback: když šablonu nenajde ve vizuálním stromu ani v App.Resources,
    /// zkusí načíst Views/Components/ComponentTemplates.xaml přes pack URI (cache).
    /// </summary>
    public sealed class FieldTemplateSelector : DataTemplateSelector
    {
        private static ResourceDictionary _templatesCache;

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is null) return null;

            var fe = container as FrameworkElement ?? Agt.Desktop.App.Current?.MainWindow;
            if (fe is null) return null;

            // 1) Modelové typy
            if (item is FieldComponentBase fcb)
            {
                var key = $"Universal_{fcb.GetType().Name}";
                return TryFind(fe, key) ?? TryFind(fe, "Universal_FieldComponentBase");
            }

            // 2) DTO s TypeKey
            var tk = TryGetTypeKey(item);
            if (!string.IsNullOrWhiteSpace(tk))
            {
                var key = $"Universal_{MapTypeKey(tk)}";
                return TryFind(fe, key) ?? TryFind(fe, "Universal_FieldComponentBase");
            }

            // 3) Poslední pokus – název typu
            {
                var key = $"Universal_{item.GetType().Name}";
                return TryFind(fe, key) ?? TryFind(fe, "Universal_FieldComponentBase");
            }
        }

        private static DataTemplate TryFind(FrameworkElement fe, string key)
        {
            // 1) ve vizuálním stromu
            var dt = fe.TryFindResource(key) as DataTemplate;
            if (dt != null) return dt;

            // 2) v Application.Resources
            dt = Agt.Desktop.App.Current?.TryFindResource(key) as DataTemplate;
            if (dt != null) return dt;

            // 3) nouzově načti slovník přímo (cache)
            if (_templatesCache == null)
            {
                try
                {
                    var uri = new Uri("pack://application:,,,/Agt.Desktop;component/Views/Components/ComponentTemplates.xaml", UriKind.Absolute);
                    _templatesCache = (ResourceDictionary)Agt.Desktop.App.LoadComponent(uri);
                }
                catch
                {
                    // ignoruj – necháme spadnout do fallbacku
                }
            }
            if (_templatesCache != null && _templatesCache.Contains(key))
                return _templatesCache[key] as DataTemplate;

            return null;
        }

        private static string TryGetTypeKey(object item)
        {
            var prop = item.GetType().GetProperty("TypeKey", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return prop?.GetValue(item) as string;
        }

        private static string MapTypeKey(string raw)
        {
            var k = (raw ?? "").Trim().ToLowerInvariant();
            return k switch
            {
                "label" => "LabelField",
                "textbox" => "TextBoxField",
                "textarea" => "TextAreaField",
                "combobox" => "ComboBoxField",
                "checkbox" => "CheckBoxField",
                "date" => "DateField",
                "number" => "NumberField",
                _ => "FieldComponentBase"
            };
        }
    }
}
