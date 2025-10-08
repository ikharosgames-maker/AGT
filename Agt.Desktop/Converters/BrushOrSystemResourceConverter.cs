using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Vrátí vstupní Brush (pokud není null). Pokud je null:
    ///   - zkusí převést string/hex přes BrushConverter
    ///   - jinak vrátí Brush z Application.Current.TryFindResource(param) (param má být systémový klíč, např. SystemColors.WindowBrushKey)
    /// </summary>
    public sealed class BrushOrSystemResourceConverter : IValueConverter
    {
        private static readonly BrushConverter _bc = new BrushConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 1) Už je to Brush? Vrať.
            if (value is Brush b) return b;

            // 2) Je to string (např. "#FF112233" nebo „Red“)? Zkus převod.
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                try
                {
                    var obj = _bc.ConvertFromString(s);
                    if (obj is Brush sb) return sb;
                }
                catch { /* ignore */ }
            }

            // 3) jinak default ze systémového resource klíče (parameter)
            if (Agt.Desktop.App.Current != null && parameter != null)
            {
                var res = Agt.Desktop.App.Current.TryFindResource(parameter);
                if (res is Brush rb) return rb;
            }

            // 4) poslední fallback – Transparent
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
