using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Agt.Desktop.Services;
using System.Windows.Input;

namespace Agt.Desktop.Views
{
    public partial class BlockLibraryBrowserWindow : Window
    {
        private readonly ObservableCollection<BlockLibEntry> _entries = new();

        public BlockLibEntry? SelectedEntry { get; private set; }

        public BlockLibraryBrowserWindow()
        {
            InitializeComponent();
            BlocksGrid.ItemsSource = _entries;
            LoadEntries();
        }

        private void LoadEntries()
        {
            _entries.Clear();

            try
            {
                var lib = Agt.Desktop.App.Services?.GetService(typeof(IBlockLibrary)) as IBlockLibrary
                          ?? BlockLibraryJson.Default;

                var all = lib.Enumerate();

                var filter = FilterBox.Text;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var f = filter.Trim();
                    all = all.Where(e =>
                        (!string.IsNullOrWhiteSpace(e.Name) && e.Name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrWhiteSpace(e.Key) && e.Key.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                foreach (var e in all.OrderBy(e => e.Name).ThenByDescending(e => e.Version))
                {
                    _entries.Add(e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Načtení knihovny bloků selhalo: " + ex.Message,
                    "Knihovna bloků",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void BlocksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BlocksGrid.SelectedItem is BlockLibEntry entry)
            {
                SelectedEntry = entry;
                DialogResult = true;
            }
        }

        private void Refresh_OnClick(object sender, RoutedEventArgs e)
        {
            LoadEntries();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            if (BlocksGrid.SelectedItem is BlockLibEntry entry)
            {
                SelectedEntry = entry;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(
                    "Vyberte blok.",
                    "Knihovna bloků",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
