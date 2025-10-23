using System;
using System.Text.Json;
using System.Windows.Media;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Sdílené měření rozměrů obsahu bloku (stejně jako editor).
    /// Vrací šířku/výšku obsahu (už s 1px rezervou, a korekcemi borderů/checkbox textu).
    /// </summary>
    public static class BlockMeasure
    {
        public static (double width, double height) Measure(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
                return (0, 0);

            try
            {
                using var doc = JsonDocument.Parse(schemaJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array)
                    return (0, 0);

                double maxRight = 0, maxBottom = 0;

                foreach (var it in items.EnumerateArray())
                {
                    var x = GetD(it, "X", 0);
                    var y = GetD(it, "Y", 0);
                    var w = GetD(it, "Width", 120);
                    var h = GetD(it, "Height", 28);
                    var typeKey = (GetS(it, "TypeKey") ?? "").Trim().ToLowerInvariant();
                    var label = GetS(it, "Label") ?? "";

                    double totalW = w, totalH = h;
                    switch (typeKey)
                    {
                        case "textbox":
                        case "textarea":
                        case "number":
                        case "date":
                        case "combobox":
                            totalW = w + 2;   // border L/R
                            totalH = h + 2;   // border T/B
                            break;

                        case "checkbox":
                            double textW = MeasureTextWidth(label, "Segoe UI", 12);
                            totalW = Math.Max(w, 20 + 4 + textW);
                            totalH = Math.Max(h, 20);
                            break;

                        default:
                            totalW = w;
                            totalH = h;
                            break;
                    }

                    maxRight = Math.Max(maxRight, x + totalW);
                    maxBottom = Math.Max(maxBottom, y + totalH);
                }

                // +1px bezpečnostní rezerva stejně jako v editoru
                return (Math.Max(1, Math.Ceiling(maxRight) + 1),
                        Math.Max(1, Math.Ceiling(maxBottom) + 1));
            }
            catch
            {
                return (0, 0);
            }
        }

        private static string? GetS(JsonElement el, string prop)
            => el.TryGetProperty(prop, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : null;

        private static double GetD(JsonElement el, string prop, double def)
        {
            if (!el.TryGetProperty(prop, out var v)) return def;
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.TryGetDouble(out var d) ? d : def,
                JsonValueKind.String => double.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                                                        System.Globalization.CultureInfo.InvariantCulture, out var d2) ? d2 : def,
                _ => def
            };
        }

#pragma warning disable CS0618
        private static double MeasureTextWidth(string text, string fontFamily, double fontSize)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var typeface = new Typeface(new FontFamily(fontFamily),
                System.Windows.FontStyles.Normal,
                System.Windows.FontWeights.Normal,
                System.Windows.FontStretches.Normal);

            var ft = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Transparent,
                1.0);
            return Math.Ceiling(ft.Width);
        }
#pragma warning restore CS0618
    }
}
