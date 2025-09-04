using System.Windows;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;


namespace Agt.Desktop;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Jednoduché "DI": vytvoříme ViewModel a nastavíme ho jako DataContext okna
        var api = new ApiClient("http://localhost:5000");
        DataContext = new FormViewModel(api);
    }
}