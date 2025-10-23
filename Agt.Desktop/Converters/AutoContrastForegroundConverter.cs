using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Vrátí explicitní foreground, pokud je k dispozici (není null/průhledný).
    /// Jinak spočítá kontrastní barvu (černá/bílá) vůči zadanému pozadí.
    /// ConverterParameter (volitelný): fallback klíč brush-e v ResourceDictionary (např. "AppTextBrush"),
    /// který se použije POUZE když není k dispozici použitelné pozadí.
    /// </summary>
    public sealed class AutoContrastForegroundConverter : IMultiValueConverter
    {
        private static double Luminance(Color c)
        {
            static double Srgb(double u) => u <= 0.03928 ? u / 12.92 : Math.Pow((u + 0.055) / 1.055, 2.4);
            var r = Srgb(c.R / 255.0);
            var g = Srgb(c.G / 255.0);
            var b = Srgb(c.B / 255.0);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static Brush GetResourceBrush(object parameter)
        {
            if (parameter is string key && !string.IsNullOrWhiteSpace(key))
            {
                var br = Agt.Desktop.App.Current?.TryFindResource(key) as Brush;
                if (br != null) return br;
            }
            return SystemColors.WindowTextBrush;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = explicit foreground (Brush nebo null)
            // values[1] = background (Brush nebo null)
            Brush explicitFg = values.Length > 0 ? values[0] as Brush : null;

            if (explicitFg is SolidColorBrush sb && sb.Color.A > 0)
                return sb; // respektuj explicitní barvu

            // Vezmi pozadí
            Brush bg = values.Length > 1 ? values[1] as Brush : null;

            if (bg is SolidColorBrush sbb && sbb.Color.A > 0)
            {
                var L = Luminance(sbb.Color);
                return L > 0.5 ? Brushes.Black : Brushes.White;
            }

            // Fallback – když není pozadí použitelné
            return GetResourceBrush(parameter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
