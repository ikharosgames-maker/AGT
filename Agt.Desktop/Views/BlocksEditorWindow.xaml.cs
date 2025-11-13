using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;
using Agt.Domain.Models;

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
                // 1) export do doménového DTO
                var def = VM.ExportBlockDefinition();

                if (def.BlockId == Guid.Empty)
                    throw new InvalidOperationException("BlockId nesmí být prázdný GUID.");

                var version = string.IsNullOrWhiteSpace(def.Version)
                    ? "1.0.0"
                    : def.Version!.Trim();
                def.Version = version;

                if (string.IsNullOrWhiteSpace(def.SchemaVersion))
                    def.SchemaVersion = "1.0";

                // 2) serializace DTO do JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var jsonIndented = JsonSerializer.Serialize(def, jsonOptions);

                using var doc = JsonDocument.Parse(jsonIndented);
                var root = doc.RootElement;

                // 3) uložit na disk (uživatelský export)
                var sfd = new SaveFileDialog
                {
                    Filter = "AGT JSON (*.json)|*.json",
                    FileName = $"{def.BlockId:D}__{version}.json"
                };

                if (sfd.ShowDialog(this) != true)
                    return;

                File.WriteAllText(sfd.FileName, jsonIndented);

                // 4) zapsat do knihovny bloků – kanonický store
                var ok = BlockLibraryJson.Default.SaveToLibrary(
                    def.BlockId,
                    version,
                    root,
                    key: def.Key,
                    blockName: def.BlockName);

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

                // 1) načíst doménové DTO (tolerantní k neznámým polím)
                var def = JsonSerializer.Deserialize<BlockDefinitionDto>(json);
                if (def == null)
                    throw new InvalidOperationException("Soubor neobsahuje platnou definici bloku.");

                // 2) naplnit designer
                VM.ImportBlockDefinition(def);
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
            VM.GridSize = VM.GridSize + 2;
        }

        private void GridMinus_OnClick(object sender, RoutedEventArgs e)
        {
            VM.GridSize = VM.GridSize > 2 ? VM.GridSize - 2 : 2;
        }

        private void AutoLayout_OnClick(object sender, RoutedEventArgs e)
        {
            VM.AutoLayout();
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
