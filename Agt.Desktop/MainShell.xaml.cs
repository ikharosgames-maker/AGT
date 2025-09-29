using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();
            DataContext = new ViewModels.CasesDashboardViewModel();
        }
    }
}
