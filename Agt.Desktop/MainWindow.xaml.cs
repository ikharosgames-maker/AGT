using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop
{
    public partial class MainWindow : Window
    {
        private DesignerViewModel VM => (DesignerViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DesignerViewModel(); // start prázdný, blok přes Menu -> Nový blok
        }

        private void NewBlock_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Views.NewBlockDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dlg.ShowDialog() == true && dlg.ResultBlock != null)
            {
                VM.NewBlock(dlg.ResultBlock);
            }
        }

        private void SaveJson_OnClick(object sender, RoutedEventArgs e)
        {
            if (VM.CurrentBlock == null) { MessageBox.Show("Nejprve založte blok."); return; }
            var sfd = new SaveFileDialog { Filter = "AGT JSON (*.json)|*.json" };
            if (sfd.ShowDialog(this) != true) return;

            var payload = VM.ExportToDto();
            File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            VM.StatusText = $"Uloženo: {System.IO.Path.GetFileName(sfd.FileName)}";
        }

        private void LoadJson_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "AGT JSON (*.json)|*.json" };
            if (ofd.ShowDialog(this) != true) return;

            try
            {
                var json = File.ReadAllText(ofd.FileName);
                VM.ImportFromDto(JsonSerializer.Deserialize<DesignerViewModel.Dto>(json)!);
                VM.StatusText = $"Načteno: {System.IO.Path.GetFileName(ofd.FileName)}";
            }
            catch
            {
                MessageBox.Show("Soubor nelze načíst.");
            }
        }
        private void GridPlus_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (DesignerViewModel)DataContext;
            vm.GridSize = vm.GridSize + 2;
        }

        private void GridMinus_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (DesignerViewModel)DataContext;
            vm.GridSize = vm.GridSize > 2 ? vm.GridSize - 2 : 2;
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e) => Close();

        private void AutoLayout_OnClick(object sender, RoutedEventArgs e) => VM.AutoLayout();
    }
}
