using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Agt.Desktop.Views;
using Agt.Infrastructure.JsonStore;

namespace Agt.Desktop
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();
        }

        private void OpenBlocksEditor(object sender, RoutedEventArgs e)
        {
            var win = new BlocksEditorWindow { Owner = this };
            win.Show();
        }

        private void OpenProcessEditor(object sender, RoutedEventArgs e)
        {
            var win = new FormProcessEditorWindow { Owner = this };
            win.Show();
        }

        private void RunCaseDemo(object sender, RoutedEventArgs e)
        {
            var win = new CaseRunWindow(Guid.NewGuid(), App.Services) { Owner = this };
            win.Show();
        }

        private void OpenBlocksFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                var folder = JsonPaths.Dir("blocks");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                var msg = "Nelze otevřít složku:" + Environment.NewLine + ex.Message;
                MessageBox.Show(this, msg, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
    }
}
