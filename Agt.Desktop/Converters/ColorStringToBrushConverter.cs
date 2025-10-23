using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Převádí string/Color/Brush -> Brush.
    /// Pokud vstup chybí nebo je nevalidní, vrátí fallback Brush z ResourceDictionary
    /// podle ConverterParameter (např. "AppTextBrush", "AppPanelBrush", "AppBackgroundBrush").
    /// </summary>
    public sealed class ColorStringToBrushConverter : IValueConverter
    {
        private static Brush GetFallback(object parameter)
        {
            var key = parameter as string;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var fromApp = Agt.Desktop.App.Current?.TryFindResource(key) as Brush;
                if (fromApp != null) return fromApp;
            }
            // Poslední pojistka – systémový textový štětec
            return SystemColors.WindowTextBrush;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1) Nic nepřišlo -> fallback
            if (value == null || value == DependencyProperty.UnsetValue)
                return GetFallback(parameter);

            // 2) Už je to Brush
            if (value is SolidColorBrush sb) return sb;
            if (value is Brush b) return b;

            // 3) Je to Color
            if (value is Color c) return new SolidColorBrush(c);

            // 4) Je to string
            if (value is string s)
            {
                s = s.Trim();

                if (string.IsNullOrEmpty(s) ||
                    s.Equals("auto", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("default", StringComparison.OrdinalIgnoreCase))
                    return GetFallback(parameter);

                try
                {
                    // Hex zápisy (#RGB, #ARGB, #RRGGBB, #AARRGGBB) nebo jmenné barvy
                    if (s.StartsWith("#", StringComparison.Ordinal))
                    {
                        var col = (Color)ColorConverter.ConvertFromString(s);
                        return new SolidColorBrush(col);
                    }
                    else
                    {
                        var prop = typeof(Colors).GetProperty(
                            s, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                        if (prop != null)
                        {
                            var col = (Color)prop.GetValue(null, null);
                            return new SolidColorBrush(col);
                        }
                    }
                }
                catch
                {
                    // ignoruj a spadni na fallback
                }
            }

            return GetFallback(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Když bys to někde potřeboval ukládat zpět, vracíme hex
            if (value is SolidColorBrush b)
                return b.Color.ToString(); // #AARRGGBB
            return Binding.DoNothing;
        }
    }
}
