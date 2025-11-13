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

        /// <summary>
        /// Pomocná funkce pro automatické zvýšení verze.
        /// Vstupní verze může být null / prázdná – v tom případě vrací "1.0.0".
        /// Jinak očekává tvar "major.minor.patch" a zvýší patch o 1.
        /// </summary>
        /// <summary>
        /// Vypočítá novou verzi podle typu změny.
        /// - první uložení → 1.0.0
        /// - změna struktury (přidání/odebrání komponent) → major++
        /// - změna layoutu/vlastností → minor++
        /// </summary>
        private static string BumpVersion(string? version, DesignerViewModel.BlockChangeKind changeKind)
        {
            // první uložení
            if (string.IsNullOrWhiteSpace(version))
                return "1.0.0";

            var parts = version.Trim().TrimStart('v', 'V').Split('.');
            int major = 0, minor = 0, patch = 0;

            if (parts.Length > 0) int.TryParse(parts[0], out major);
            if (parts.Length > 1) int.TryParse(parts[1], out minor);
            if (parts.Length > 2) int.TryParse(parts[2], out patch);

            switch (changeKind)
            {
                case DesignerViewModel.BlockChangeKind.Structure:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;

                case DesignerViewModel.BlockChangeKind.LayoutOrProperties:
                    minor++;
                    patch = 0;
                    break;

                case DesignerViewModel.BlockChangeKind.None:
                default:
                    // nedetekovaná změna → necháme původní verzi beze změny
                    return $"{major}.{minor}.{patch}";
            }

            return $"{major}.{minor}.{patch}";
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
                // 1) export do doménového DTO (aktuální stav)
                var def = VM.ExportBlockDefinition();

                if (def.BlockId == Guid.Empty)
                    throw new InvalidOperationException("BlockId nesmí být prázdný GUID.");

                // Rozhodnout typ změny vůči poslednímu uloženému/načtenému stavu
                var changeKind = VM.GetChangeKind(def);

                // Chytré verzování: major/minor podle typu změny
                var newVersion = BumpVersion(def.Version, changeKind);
                def.Version = newVersion;

                if (string.IsNullOrWhiteSpace(def.SchemaVersion))
                    def.SchemaVersion = "1.0";

                if (string.IsNullOrWhiteSpace(def.BlockName))
                    def.BlockName = "(bez názvu)";

                // Autor + čas založení:
                if (string.IsNullOrWhiteSpace(def.CreatedBy))
                    def.CreatedBy = Environment.UserName;

                if (def.CreatedAt == default)
                    def.CreatedAt = DateTime.UtcNow;

                // 2) serializace DTO do JSON
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonIndented = JsonSerializer.Serialize(def, jsonOptions);

                using var doc = JsonDocument.Parse(jsonIndented);
                var root = doc.RootElement;

                // 3) uložení pouze do knihovny bloků
                var ok = BlockLibraryJson.Default.SaveToLibrary(
                    def.BlockId,
                    newVersion,
                    root,
                    key: def.Key,
                    blockName: def.BlockName);

                if (!ok)
                    throw new InvalidOperationException("Zápis do knihovny bloků selhal.");

                // aktualizovat stav ve viewmodelu (pro další uložení)
                VM.CurrentVersion = newVersion;
                VM.CurrentCreatedBy = def.CreatedBy;
                VM.CurrentCreatedAt = def.CreatedAt;
                VM.MarkSaved(def);

                VM.StatusText = $"Uloženo do knihovny: {def.BlockName} v{newVersion} (autor: {def.CreatedBy})";

                MessageBox.Show(
                    $"Blok byl uložen do knihovny.\n\n" +
                    $"Název:  {def.BlockName}\n" +
                    $"Verze:  {newVersion}\n" +
                    $"Autor:  {def.CreatedBy}\n" +
                    $"BlockId: {def.BlockId}",
                    "Uloženo",
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
            var win = new BlockLibraryBrowserWindow
            {
                Owner = this
            };

            if (win.ShowDialog() != true || win.SelectedEntry == null)
                return;

            try
            {
                var lib = Agt.Desktop.App.Services?.GetService(typeof(IBlockLibrary)) as IBlockLibrary
                          ?? BlockLibraryJson.Default;

                if (!lib.TryLoadByIdVersion(win.SelectedEntry.BlockId, win.SelectedEntry.Version,
                                            out var doc, out var entry) || doc == null)
                {
                    MessageBox.Show(
                        "Vybraný blok se nepodařilo načíst z knihovny.",
                        "Knihovna bloků",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                using (doc)
                {
                    var json = doc.RootElement.GetRawText();
                    var def = JsonSerializer.Deserialize<BlockDefinitionDto>(json);
                    if (def == null)
                        throw new InvalidOperationException("JSON neobsahuje platnou definici bloku.");

                    VM.ImportBlockDefinition(def);
                    VM.StatusText = $"Načteno z knihovny: {def.BlockName} v{def.Version}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Blok nelze načíst:\n{ex.Message}",
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
