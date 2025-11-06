using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    public partial class StartCaseDialog : Window
    {
        public StartCaseDialog() { InitializeComponent(); }

        public IReadOnlyList<string> SelectedStartBlockKeys { get; private set; } = Array.Empty<string>();

        // Handler volaný z XAML (SelectionChanged u verze)
        private void VersionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: pøípadná logika pro zmìnu verze; skeleton nechává prázdné.
        }

        // Handler volaný z XAML (Click na OK)
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
