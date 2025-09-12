using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Services;

namespace Agt.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeService.ApplyThemeResources(Current);

            var win = new MainWindow();
            win.Show();
        }
    }
}
