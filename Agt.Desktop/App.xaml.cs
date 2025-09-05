using System.Windows;
using Agt.Desktop.Services;
using Agt.Desktop.Converters;

namespace Agt.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 1) Nejprve zaregistrujeme všechno, co XAML potřebuje
            Resources["SelectionService"] = new SelectionService();
            Resources["FieldCatalog"] = new FieldCatalogService();
            Resources["FieldFactory"] = new FieldFactory();

            Resources["BoolToVisibility"] = new BoolToVisibilityConverter();
            Resources["CountGte"] = new CountGreaterOrEqualConverter();
            Resources["CountToVisibility"] = new CountToVisibilityConverter();

            // 2) Až teď vytvoříme hlavní okno – XAML už Statické zdroje najde
            var main = new MainWindow();
            main.Show();

            base.OnStartup(e);
        }
    }
}
