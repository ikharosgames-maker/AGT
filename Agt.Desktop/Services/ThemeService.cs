using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace Agt.Desktop.Services
{
    public static class ThemeService
    {
        public static bool IsLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var v = key?.GetValue("AppsUseLightTheme");
                if (v is int i) return i != 0;
            }
            catch { }
            return true;
        }

        public static void ApplyThemeResources(Agt.Desktop.App app)
        {
            bool light = IsLightTheme();

            var canvas = light ? Color.FromRgb(0xFA, 0xFA, 0xFA) : Color.FromRgb(0x20, 0x20, 0x20);
            var field = light ? Colors.White : Color.FromRgb(0x2E, 0x2E, 0x2E);
            var text = light ? Colors.Black : Colors.White;
            var gridPt = light ? Color.FromArgb(0x50, 0, 0, 0) : Color.FromArgb(0x40, 255, 255, 255);

            // Menu
            var menuBg = light ? Color.FromRgb(0xF3, 0xF3, 0xF3) : Color.FromRgb(0x2A, 0x2A, 0x2A);
            var menuFg = light ? Colors.Black : Colors.White;
            var menuHover = light ? Color.FromRgb(0xE5, 0xE5, 0xE5) : Color.FromRgb(0x40, 0x40, 0x40);

            app.Resources["CanvasBackgroundBrush"] = new SolidColorBrush(canvas);
            app.Resources["FieldBackgroundBrush"] = new SolidColorBrush(field);
            app.Resources["ControlTextBrush"] = new SolidColorBrush(text);
            app.Resources["GridDotBrush"] = new SolidColorBrush(gridPt);

            app.Resources["MenuBackgroundBrush"] = new SolidColorBrush(menuBg);
            app.Resources["MenuTextBrush"] = new SolidColorBrush(menuFg);
            app.Resources["MenuHoverBrush"] = new SolidColorBrush(menuHover);
        }
    }
}
