using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Z Brush -> HEX s fallbackem na theme brush (ConverterParameter = klíč brush v resourcích, např. "AppTextBrush").
    /// ConvertBack: z HEX -> SolidColorBrush; pokud je prázdný text, vrací null (tj. „ber z tématu“).
    /// </summary>
    public sealed class BrushToHexThemeFallbackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush? brush = value as Brush;

            if (brush == null && parameter is string key)
            {
                var fallback = Agt.Desktop.App.Current.TryFindResource(key) as SolidColorBrush;
                brush = fallback;
            }

            if (brush is SolidColorBrush scb)
            {
                return $"#{scb.Color.R:X2}{scb.Color.G:X2}{scb.Color.B:X2}";
            }

            return string.Empty; // nic lepšího nevymyslíme
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim();
            if (string.IsNullOrEmpty(s))
                return null; // „reset“ = ber z tématu

            // povolit formáty #RGB, #RRGGBB, #AARRGGBB
            try
            {
                if (!s.StartsWith("#")) s = "#" + s;
                var color = (Color)ColorConverter.ConvertFromString(s);
                return new SolidColorBrush(color);
            }
            catch
            {
                return DependencyProperty.UnsetValue; // nevalidní vstup → nepropagovat
            }
        }
    }
}
