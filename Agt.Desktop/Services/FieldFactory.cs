using System.Collections.Generic;
using System.Text.Json;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public class FieldFactory
    {
        public FieldComponentBase Create(string key, double x, double y, Dictionary<string, object>? defaults = null)
        {
            FieldComponentBase f = key switch
            {
                "label" => new LabelField(),
                "textbox" => new TextBoxField(),
                "textarea" => new TextAreaField(),
                "combobox" => new ComboBoxField(),
                "checkbox" => new CheckBoxField(),
                "date" => new DateField(),
                "number" => new NumberField(),
                _ => new TextBoxField() // fallback
            };

            // umístění
            f.X = x; f.Y = y;

            // společné defaulty
            SetIf<string>(defaults, "FieldKey", v => f.FieldKey = v);
            SetIf<string>(defaults, "Label", v => f.Label = v);
            SetIf<bool>(defaults, "Required", v => f.Required = v);
            SetIf<string>(defaults, "Placeholder", v => f.Placeholder = v);
            SetIf<string>(defaults, "DefaultValue", v => f.DefaultValue = v);
            SetIf<double>(defaults, "Width", v => f.Width = v);
            SetIf<double>(defaults, "Height", v => f.Height = v);

            switch (f)
            {
                case LabelField lf:
                    SetIf<double>(defaults, "FontSize", v => lf.FontSize = v);
                    break;

                case TextBoxField tbf:
                    SetIf<int>(defaults, "MaxLength", v => tbf.MaxLength = v);
                    break;

                case TextAreaField taf:
                    SetIf<int>(defaults, "Rows", v => taf.Rows = v);
                    break;

                case ComboBoxField cb:
                    if (defaults != null && defaults.TryGetValue("Options", out var raw) && raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var it in je.EnumerateArray())
                            cb.Options.Add(it.ToString());
                    }
                    SetIf<bool>(defaults, "IsEditable", v => cb.IsEditable = v);
                    break;

                case CheckBoxField ch:
                    SetIf<bool>(defaults, "IsCheckedDefault", v => ch.IsCheckedDefault = v);
                    break;

                case DateField df:
                    SetIf<string>(defaults, "Format", v => df.Format = v);
                    break;

                case NumberField nf:
                    SetIf<double>(defaults, "Min", v => nf.Min = v);
                    SetIf<double>(defaults, "Max", v => nf.Max = v);
                    SetIf<int>(defaults, "Decimals", v => nf.Decimals = v);
                    break;
            }

            return f;
        }

        private static void SetIf<T>(Dictionary<string, object>? dict, string key, System.Action<T> setter)
        {
            if (dict == null) return;
            if (!dict.TryGetValue(key, out var raw) || raw is null) return;

            try
            {
                object converted = raw;

                if (typeof(T) == typeof(double))
                {
                    if (raw is double d) converted = d;
                    else if (raw is JsonElement je && je.TryGetDouble(out var jd)) converted = jd;
                    else if (double.TryParse(raw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pd)) converted = pd;
                }
                else if (typeof(T) == typeof(int))
                {
                    if (raw is int i) converted = i;
                    else if (raw is JsonElement je && je.TryGetInt32(out var ji)) converted = ji;
                    else if (int.TryParse(raw.ToString(), out var pi)) converted = pi;
                }
                else if (typeof(T) == typeof(bool))
                {
                    if (raw is bool b) converted = b;
                    else if (raw is JsonElement je && je.ValueKind == JsonValueKind.True) converted = true;
                    else if (raw is JsonElement je2 && je2.ValueKind == JsonValueKind.False) converted = false;
                    else if (bool.TryParse(raw.ToString(), out var pb)) converted = pb;
                }
                else if (typeof(T) == typeof(string))
                {
                    if (raw is JsonElement je && je.ValueKind != JsonValueKind.String) converted = raw.ToString();
                }

                setter((T)converted);
            }
            catch { /*ignore*/ }
        }
    }
}
