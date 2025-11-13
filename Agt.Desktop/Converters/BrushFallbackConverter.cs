using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Converters
{
    /// <summary>
    /// Vrátí vstupní Brush (pokud je), jinak zkusí najít první existující resource dle klíčů.
    /// Použití v XAML:
    /// Background="{Binding Background, Converter={StaticResource BrushFallback}, ConverterParameter=AppControlBackgroundBrush|EditorPanelBrush}"
    /// </summary>
    public sealed class BrushFallbackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Brush b) return b;

            var keys = (parameter as string ?? string.Empty)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim());

            foreach (var key in keys)
            {
                var res = TryFindResource(key);
                if (res is Brush brush) return brush;
            }

            // poslední jistota – systémové okno/Control
            return SystemColors.WindowBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        private static object TryFindResource(object key)
        {
            // Hledání v aktuální aplikaci (nejširší scope)
            if (Agt.Desktop.App.Current != null)
            {
                var found = Agt.Desktop.App.Current.TryFindResource(key);
                if (found != null) return found;
            }
            return null;
        }
    }
}
