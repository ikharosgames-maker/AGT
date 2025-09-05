using System.Windows;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DesignerViewModel();
        }
    }
}
