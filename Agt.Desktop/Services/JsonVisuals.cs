using System.Text.Json.Nodes;
using System.Windows.Media;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Přenos vizuálních vlastností FieldComponentBase do/z JSON (flat: Background, Foreground, FontFamily, FontSize).
    /// Bez fallbacků – používá se přesně to, co je v JSON/modelu.
    /// </summary>
    public static class JsonVisuals
    {
        public static void Read(FieldComponentBase c, JsonObject node)
        {
            if (node.TryGetPropertyValue("Foreground", out var fg) && fg is JsonValue v1 && v1.TryGetValue<string>(out var fgHex))
                c.Foreground = ToBrush(fgHex);

            if (node.TryGetPropertyValue("Background", out var bg) && bg is JsonValue v2 && v2.TryGetValue<string>(out var bgHex))
                c.Background = ToBrush(bgHex);

            if (node.TryGetPropertyValue("FontFamily", out var ff) && ff is JsonValue v3 && v3.TryGetValue<string>(out var fam) && !string.IsNullOrWhiteSpace(fam))
                c.FontFamily = fam;

            if (node.TryGetPropertyValue("FontSize", out var fs) && fs is JsonValue v4 && v4.TryGetValue<double>(out var size) && size > 0)
                c.FontSize = size;
        }

        public static void Write(FieldComponentBase c, JsonObject node)
        {
            if (c.Foreground is SolidColorBrush f1) node["Foreground"] = ToHex(f1.Color);
            if (c.Background is SolidColorBrush f2) node["Background"] = ToHex(f2.Color);
            if (!string.IsNullOrWhiteSpace(c.FontFamily)) node["FontFamily"] = c.FontFamily;
            if (c.FontSize > 0) node["FontSize"] = c.FontSize;
        }

        // --- helpers ---
        private static SolidColorBrush? ToBrush(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            try
            {
                var col = (Color)ColorConverter.ConvertFromString(hex)!;
                return new SolidColorBrush(col);
            }
            catch { return null; }
        }

        private static string ToHex(Color c)
            => (c.A == 255) ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
