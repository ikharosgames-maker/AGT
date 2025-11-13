using System;
using System.Text.Json.Nodes;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public static class ComponentJsonMapper
    {
        public static FieldComponentBase FromJson(JsonObject item)
        {
            var typeKey = (item["TypeKey"]?.GetValue<string>() ?? "").Trim().ToLowerInvariant();

            FieldComponentBase comp = typeKey switch
            {
                "label" => new LabelField(),
                "textbox" => new TextBoxField(),
                "textarea" => new TextAreaField(),
                "number" => new NumberField(),
                "date" => new DateField(),
                "combobox" => new ComboBoxField(),
                "checkbox" => new CheckBoxField(),
                _ => new LabelField()
            };

            comp.TypeKey = typeKey;
            comp.Id = Guid.TryParse(item["Id"]?.GetValue<string>(), out var gid) ? gid : Guid.NewGuid();
            comp.Name = item["Name"]?.GetValue<string>() ?? "";
            comp.FieldKey = item["FieldKey"]?.GetValue<string>() ?? "";
            comp.Label = item["Label"]?.GetValue<string>() ?? "";

            comp.X = item["X"]?.GetValue<double>() ?? 0;
            comp.Y = item["Y"]?.GetValue<double>() ?? 0;
            comp.Width = item["Width"]?.GetValue<double>() ?? 120;
            comp.Height = item["Height"]?.GetValue<double>() ?? 28;
            comp.ZIndex = item["ZIndex"]?.GetValue<int>() ?? 0;

            comp.DefaultValue = item.ContainsKey("DefaultValue") ? item["DefaultValue"]?.ToString() : null;

            // VIZUÁL
            JsonVisuals.Read(comp, item);

            // Typové specifikum: ComboBox options
            if (comp is ComboBoxField cb && item.TryGetPropertyValue("Options", out var opts) && opts is JsonArray arr)
            {
                foreach (var n in arr)
                {
                    if (n is JsonValue v && v.TryGetValue<string>(out var s))
                        cb.Options.Add(s);
                }
            }

            return comp;
        }

        public static JsonObject ToJson(FieldComponentBase comp)
        {
            var node = new JsonObject
            {
                ["TypeKey"] = comp.TypeKey,
                ["Id"] = comp.Id.ToString(),
                ["Name"] = comp.Name,
                ["FieldKey"] = comp.FieldKey,
                ["Label"] = comp.Label,
                ["X"] = comp.X,
                ["Y"] = comp.Y,
                ["Width"] = comp.Width,
                ["Height"] = comp.Height,
                ["ZIndex"] = comp.ZIndex,
                ["DefaultValue"] = comp.DefaultValue
            };

            JsonVisuals.Write(comp, node);

            if (comp is ComboBoxField cb)
            {
                var arr = new JsonArray();
                foreach (var s in cb.Options) arr.Add(s);
                node["Options"] = arr;
            }

            return node;
        }
    }
}
