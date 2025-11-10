using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Dialog pro start case. Using direktivy jsou nahoře a kód je uvnitř třídy/namespace.
    /// </summary>
    public partial class StartCaseDialog : Window
    {
        public StartCaseDialog()
        {
            InitializeComponent();
        }

        public IReadOnlyList<string> SelectedStartBlockKeys { get; private set; } = Array.Empty<string>();

        private static IReadOnlyList<string> PickInitialBlocksFromFirstStage(/* LayoutModel layout */)
        {
            return Array.Empty<string>();
        }

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}