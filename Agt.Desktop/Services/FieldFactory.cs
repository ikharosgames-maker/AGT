using System;
using System.Globalization;
using System.Windows.Media;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public static class FieldFactory
    {
        private static readonly Brush DefaultForeground = Brushes.Black;
        private const string DefaultFontFamily = "Segoe UI";
        private const double DefaultFontSize = 12;

        private static T ApplyVisuals<T>(T c) where T : FieldComponentBase
        {
            // Background ponecháváme (default = Transparent)
            c.Foreground = DefaultForeground.Clone();
            c.FontFamily = DefaultFontFamily;
            c.FontSize = DefaultFontSize;
            return c;
        }

        private static T ApplyPositionAndSize<T>(T c, double? x, double? y, double? width, double? height) where T : FieldComponentBase
        {
            if (x.HasValue) c.X = x.Value;
            if (y.HasValue) c.Y = y.Value;
            if (width.HasValue) c.Width = width.Value;
            if (height.HasValue) c.Height = height.Value;
            c.TotalWidth = c.Width;
            c.TotalHeight = c.Height;
            return c;
        }

        // ---- Pomocné převody z object? na double? (akceptuje null, čísla, stringy) ----
        private static double? ToDoubleOrNull(object? v)
        {
            if (v is null) return null;

            switch (v)
            {
                case double d: return d;
                case float f: return (double)f;
                case int i: return i;
                case long l: return l;
                case decimal m: return (double)m;
                case string s:
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                        return parsed;
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                        return parsed;
                    return null;
                default:
                    return null;
            }
        }

        // ===== Jednotná Create(string) – volání bez souřadnic/rozměrů =====
        public static FieldComponentBase Create(string typeKey)
            => CreateInternal(typeKey, null, null, null, null);

        // ===== Přetížení s 4 parametry (kompatibilita s DesignerViewModel) =====
        // Signatura s object? umožní posílat null i „objektové“ šířky z bindingů/VM.
        public static FieldComponentBase Create(string typeKey, double x, double y, object? width)
            => CreateInternal(typeKey, x, y, ToDoubleOrNull(width), null);

        // ===== Přetížení s 5 parametry – explicitní rozměry (object?) =====
        public static FieldComponentBase Create(string typeKey, double x, double y, object? width, object? height)
            => CreateInternal(typeKey, x, y, ToDoubleOrNull(width), ToDoubleOrNull(height));

        // ===== Interní jednotná logika =====
        private static FieldComponentBase CreateInternal(string typeKey, double? x, double? y, double? width, double? height)
        {
            if (string.IsNullOrWhiteSpace(typeKey))
                throw new ArgumentOutOfRangeException(nameof(typeKey), "Neznámý typ komponenty");

            var t = typeKey.Trim()
                           .Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                           .ToLower(CultureInfo.InvariantCulture);

            switch (t)
            {
                case "textbox":
                case "text":
                case "input":
                    {
                        var c = ApplyVisuals(new TextBoxField { Label = "Text" });
                        height ??= 50;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                case "textarea":
                case "multiline":
                    {
                        var c = ApplyVisuals(new TextAreaField { Label = "Víceřádkový text" });
                        height ??= 100;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                case "number":
                case "numeric":
                case "int":
                case "double":
                    {
                        var c = ApplyVisuals(new NumberField { Label = "Číslo" });
                        height ??= 50;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                case "date":
                case "datepicker":
                case "datum":
                    {
                        var c = ApplyVisuals(new DateField { Label = "Datum" });
                        height ??= 50;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                // ve switch (t) přidej tento case do skupiny comboboxu:
                case "combo":
                case "combobox":
                case "select":
                case "dropdown":
                    {
                        var c = ApplyVisuals(new ComboBoxField
                        {
                            Label = "Výběr",
                            IsEditable = false,
                            Background = Brushes.White   // ← přidáno: neprůhledný default uvnitř kontrolu
                        });
                        height ??= 50;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                case "checkbox":
                case "check":
                case "bool":
                    {
                        var c = ApplyVisuals(new CheckBoxField { Label = "Zaškrtnout", IsCheckedDefault = false });
                        height ??= 40;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                case "label":
                case "textlabel":
                case "caption":
                    {
                        var c = ApplyVisuals(new LabelField { Label = "Popisek" });
                        height ??= 40;
                        return ApplyPositionAndSize(c, x, y, width, height);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeKey), "Neznámý typ komponenty");
            }
        }

        // ===== Volitelné specializované továrny (ponecháno kvůli kompatibilitě) =====
        public static TextBoxField CreateTextBox(string label = "", string placeholder = "")
            => ApplyVisuals(new TextBoxField { Label = label, Placeholder = placeholder, Width = 240, Height = 60 });

        public static TextAreaField CreateTextArea(string label = "", string placeholder = "")
            => ApplyVisuals(new TextAreaField { Label = label, Placeholder = placeholder, Width = 240, Height = 100 });

        public static NumberField CreateNumber(string label = "")
            => ApplyVisuals(new NumberField { Label = label, Width = 160, Height = 60 });

        public static DateField CreateDate(string label = "")
            => ApplyVisuals(new DateField { Label = label, Width = 180, Height = 60 });

        public static ComboBoxField CreateCombo(string label = "", bool isEditable = false)
            => ApplyVisuals(new ComboBoxField { Label = label, IsEditable = isEditable, Width = 220, Height = 60 });

        public static CheckBoxField CreateCheck(string label = "", bool isChecked = false)
            => ApplyVisuals(new CheckBoxField { Label = label, IsCheckedDefault = isChecked, Width = 220, Height = 40 });

        public static LabelField CreateLabel(string text = "")
            => ApplyVisuals(new LabelField { Label = text, Width = 220, Height = 40 });
    }
}
