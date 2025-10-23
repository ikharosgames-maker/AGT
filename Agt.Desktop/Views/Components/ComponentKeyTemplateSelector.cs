using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Hledá DataTemplate podle klíče {Prefix}_{TypeName}, kde Prefix = RO | Run | Edit.
    /// Primární zdroj: TypeKey (z repozitáře). Fallback: Type/FieldType/Kind/ComponentType nebo runtime typ.
    /// </summary>
    public sealed class ComponentKeyTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is null || container is null) return null;

            var fe = container as FrameworkElement;
            var viewer = FindAncestor<ComponentViewerControl>(fe);
            var mode = viewer?.Mode ?? RenderMode.ReadOnly;

            var prefix = mode switch
            {
                RenderMode.Edit => "Edit",
                RenderMode.Run => "Run",
                _ => "RO"
            };

            // typ z dat (TypeKey → "textbox", "label", ...)
            var rawKey = ReadString(item, "TypeKey")
                      ?? ReadString(item, "Type")
                      ?? ReadString(item, "FieldType")
                      ?? ReadString(item, "Kind")
                      ?? ReadString(item, "ComponentType")
                      ?? item.GetType().Name;

            var (full, shortName) = NormalizeTypeKey(rawKey);

            // kandidáti klíčů
            foreach (var k in new[]
            {
    $"{prefix}_{full}",
    $"{prefix}_{shortName}",
    $"Universal_{full}",
    $"Universal_{shortName}",
    $"{prefix}_FieldComponentBase",
    "Universal_FieldComponentBase"
})
            {
                var dt = TryFindTemplate(fe, k);
                if (dt != null) return dt;
            }


            // fallback
            return TryFindTemplate(fe, $"{prefix}_FieldComponentBase");
        }

        private static (string full, string shortName) NormalizeTypeKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return ("FieldComponentBase", "FieldComponentBase");

            var s = raw.Trim();

            var comma = s.IndexOf(',');
            if (comma > 0) s = s[..comma];
            var dot = s.LastIndexOf('.');
            if (dot >= 0 && dot < s.Length - 1) s = s[(dot + 1)..];

            s = s.Replace("-", "").Replace("_", "").Trim();
            var low = s.ToLowerInvariant();

            string normalized = low switch
            {
                "label" => "LabelField",
                "textbox" => "TextBoxField",
                "text" => "TextBoxField",
                "textarea" => "TextAreaField",
                "number" => "NumberField",
                "date" => "DateField",
                "datetime" => "DateField",
                "checkbox" => "CheckBoxField",
                "check" => "CheckBoxField",
                "combobox" => "ComboBoxField",
                "select" => "ComboBoxField",
                _ => s.EndsWith("Field", StringComparison.Ordinal) ? s : s + "Field"
            };

            var shortName = normalized.EndsWith("Field", StringComparison.Ordinal)
                ? normalized[..^"Field".Length]
                : normalized;

            return (normalized, shortName);
        }

        private static DataTemplate TryFindTemplate(FrameworkElement fe, string key)
        {
            if (fe != null && fe.TryFindResource(key) is DataTemplate dt1) return dt1;
            if (fe != null)
            {
                var viewer = FindAncestor<ComponentViewerControl>(fe);
                if (viewer != null && viewer.Resources.Contains(key))
                    return viewer.Resources[key] as DataTemplate;
            }
            if (Agt.Desktop.App.Current != null && Agt.Desktop.App.Current.Resources.Contains(key))
                return Agt.Desktop.App.Current.Resources[key] as DataTemplate;
            return null;
        }

        private static string ReadString(object o, string prop)
        {
            var pi = o.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            return (pi != null && pi.PropertyType == typeof(string)) ? (string)pi.GetValue(o) : null;
        }

        private static T FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            while (d != null)
            {
                if (d is T t) return t;
                d = LogicalTreeHelper.GetParent(d) ?? System.Windows.Media.VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
