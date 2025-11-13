using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;

namespace Agt.Desktop.Views
{
    public partial class BlocksEditorWindow : Window
    {
        private DesignerViewModel VM => (DesignerViewModel)DataContext;

        public BlocksEditorWindow()
        {
            InitializeComponent();
            DataContext = new DesignerViewModel();
        }

        private void NewBlock_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new NewBlockDialog
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
            if (VM.CurrentBlock == null)
            {
                MessageBox.Show(
                    "Nejprve založte nebo načtěte blok.",
                    "Upozornění",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                // Export DTO → z něj uděláme JSON a vyčteme BlockId/Version/Key/BlockName z kořene
                var dto = VM.ExportToDto();
                if (dto == null)
                    throw new InvalidOperationException("Export vrací prázdná data.");

                var jsonIndented = JsonSerializer.Serialize(
                    dto,
                    new JsonSerializerOptions { WriteIndented = true });

                using var doc = JsonDocument.Parse(jsonIndented);
                var root = doc.RootElement;

                // BlockId (povinné)
                if (!root.TryGetProperty("BlockId", out var blockIdEl) ||
                    !Guid.TryParse(blockIdEl.GetString(), out var blockId) ||
                    blockId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "Kořen JSON neobsahuje platný 'BlockId' (GUID).");
                }

                // Version (povinné – ale když chybí, doplníme 1.0.0)
                string version = root.TryGetProperty("Version", out var verEl)
                    ? (verEl.GetString() ?? "")
                    : "";
                if (string.IsNullOrWhiteSpace(version))
                    version = "1.0.0";

                // Volitelné
                string? key = root.TryGetProperty("Key", out var keyEl)
                    ? keyEl.GetString()
                    : null;

                string? blockName = root.TryGetProperty("BlockName", out var nameEl)
                    ? nameEl.GetString()
                    : null;

                // 1) uložit do vybraného souboru
                var sfd = new SaveFileDialog
                {
                    Filter = "AGT JSON (*.json)|*.json",
                    FileName = $"{blockId:D}__{version}.json"
                };

                if (sfd.ShowDialog(this) != true)
                    return;

                File.WriteAllText(sfd.FileName, jsonIndented);

                // 2) zapsat do knihovny bloků (KANON: BlockId + Version, soubor {BlockId}__{Version}.json)
                var ok = BlockLibraryJson.Default.SaveToLibrary(
                    blockId,
                    version,
                    root,
                    key: key,
                    blockName: blockName);

                if (!ok)
                    throw new InvalidOperationException("Zápis do knihovny bloků selhal.");

                VM.StatusText = $"Uloženo: {Path.GetFileName(sfd.FileName)}";

                MessageBox.Show(
                    "Blok byl úspěšně uložen a zapsán do knihovny.",
                    "Hotovo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ukládání selhalo:\n{ex.Message}",
                    "Chyba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadJson_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "AGT JSON (*.json)|*.json"
            };
            if (ofd.ShowDialog(this) != true)
                return;

            try
            {
                var json = File.ReadAllText(ofd.FileName);

                // TADY byla chyba – chyběl typ pro deserializaci
                var dto = JsonSerializer.Deserialize<DesignerViewModel.Dto>(json);

                if (dto == null)
                    throw new InvalidOperationException("Soubor neobsahuje platný blok.");

                VM.ImportFromDto(dto);
                VM.StatusText = $"Načteno: {Path.GetFileName(ofd.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Soubor nelze načíst:\n{ex.Message}",
                    "Chyba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
