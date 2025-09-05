using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Agt.Desktop.Models;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop
{
    public partial class MainWindow : Window
    {
        private DesignerViewModel VM => (DesignerViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DesignerViewModel();

            // Vyžádej nový blok při startu (nemá-li už nějaký)
            if (VM.CurrentBlock == null)
                NewBlock();
        }

        private void NewBlock_OnClick(object sender, RoutedEventArgs e) => NewBlock();

        private void NewBlock()
        {
            var dlg = new Views.NewBlockDialog { Owner = this };
            if (dlg.ShowDialog() == true && dlg.ResultBlock != null)
            {
                VM.CurrentBlock = dlg.ResultBlock;
                VM.Items.Clear();
                VM.StatusText = $"Založen blok {VM.CurrentBlock.Name} ({VM.CurrentBlock.Id})";
            }
            else
            {
                if (VM.CurrentBlock == null)
                    Close();
            }
        }

        private void SaveJson_OnClick(object sender, RoutedEventArgs e)
        {
            if (VM.CurrentBlock == null) { MessageBox.Show("Nejprve založte blok."); return; }
            var sfd = new SaveFileDialog { Filter = "AGT JSON (*.json)|*.json" };
            if (sfd.ShowDialog(this) != true) return;

            var payload = VM.ExportToDto();
            File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            VM.StatusText = $"Uloženo: {Path.GetFileName(sfd.FileName)}";
        }

        private void LoadJson_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "AGT JSON (*.json)|*.json" };
            if (ofd.ShowDialog(this) != true) return;

            try
            {
                var json = File.ReadAllText(ofd.FileName);
                VM.ImportFromDto(JsonSerializer.Deserialize<DesignerViewModel.Dto>(json)!);
                VM.StatusText = $"Načteno: {Path.GetFileName(ofd.FileName)}";
            }
            catch
            {
                MessageBox.Show("Soubor nelze načíst.");
            }
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e) => Close();

        private void GridSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && double.TryParse(mi.Header.ToString(), out var size))
                VM.GridSize = size;
        }

        private void AutoLayout_OnClick(object sender, RoutedEventArgs e)
        {
            VM.AutoLayout(); // jednoduchý návrh – rozloží do sloupců
        }
    }
}
