using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public class FieldFactory
    {
        public FieldComponentBase Create(string key, double x, double y, object? defaults)
        {
            FieldComponentBase i = key switch
            {
                "label" => new LabelField(),
                "textbox" => new TextBoxField(),
                "textarea" => new TextAreaField(),
                "combobox" => new ComboBoxField(),
                "checkbox" => new CheckBoxField(),
                "date" => new DateField(),
                "number" => new NumberField(),
                _ => new TextBoxField()
            };

            i.X = x; i.Y = y;

            if (defaults != null)
                ApplyDefaults(i, defaults);

            return i;
        }

        private static void ApplyDefaults(FieldComponentBase target, object defaults)
        {
            var tProps = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var dType = defaults.GetType();

            foreach (var d in dType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var tp = tProps.FirstOrDefault(p => string.Equals(p.Name, d.Name, StringComparison.OrdinalIgnoreCase));
                if (tp == null || !tp.CanWrite) continue;

                var val = d.GetValue(defaults);
                if (val is IEnumerable<string> strEnum &&
                    tp.PropertyType.IsGenericType &&
                    tp.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ObservableCollection<>))
                {
                    var addMethod = tp.PropertyType.GetMethod("Add");
                    var inst = Activator.CreateInstance(tp.PropertyType);
                    foreach (var s in strEnum) addMethod!.Invoke(inst, new object?[] { s });
                    tp.SetValue(target, inst);
                    continue;
                }

                tp.SetValue(target, val);
            }
        }
    }
}
