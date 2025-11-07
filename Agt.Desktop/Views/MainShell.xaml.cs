using System;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();
        }

        private void OnRunCase(object sender, RoutedEventArgs e)
        {
            var win = new CaseRunWindow(Guid.NewGuid(), App.Services);
            win.Owner = this;
            win.ShowDialog();
        }
    }
}
