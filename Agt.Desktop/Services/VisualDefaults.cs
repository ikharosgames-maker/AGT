using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Jediný zdroj pravdy pro výchozí vizuální hodnoty komponent.
    /// Nepoužívá konvertory ani TargetNullValue – data se vždy doplní.
    /// </summary>
    public static class VisualDefaults
    {
        private static Brush GetBrush(string key, Brush fallback)
        {
            var res = Agt.Desktop.App.Current?.Resources;
            if (res != null && res[key] is Brush b) return b;
            return fallback;
        }

        /// <summary>Doplní chybějící hodnoty (ponechá už zadané).</summary>
        public static void ApplyMissing(FieldComponentBase f)
        {
            if (f.Foreground == null) f.Foreground = GetBrush("AppTextBrush", Brushes.Black);
            if (f.Background == null) f.Background = GetBrush("AppPanelAltBrush", Brushes.White);
            if (string.IsNullOrWhiteSpace(f.FontFamily)) f.FontFamily = "Segoe UI";
            if (f.FontSize <= 0) f.FontSize = 12d;
        }
    }
}
