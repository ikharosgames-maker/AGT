using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class BlockLibraryWindow : Window
    {
        public ObservableCollection<BlockItem> Items { get; } = new();

        public BlockLibraryWindow()
        {
            InitializeComponent();
            DataContext = this;

            Items.Add(new BlockItem { Id = Guid.NewGuid(), Title = "TextBox", Version = "1.0.0" });
            Items.Add(new BlockItem { Id = Guid.NewGuid(), Title = "ComboBox", Version = "1.0.0" });
            Items.Add(new BlockItem { Id = Guid.NewGuid(), Title = "Checkbox", Version = "1.0.0" });

            BlocksList.ItemsSource = Items;
            BlocksList.SelectionChanged += (_, __) => LoadDetail();
            if (Items.Count > 0) BlocksList.SelectedIndex = 0;
        }

        private void LoadDetail()
        {
            var it = BlocksList.SelectedItem as BlockItem;
            if (it == null) return;
            TitleBox.Text = it.Title;
            VersionBox.Text = it.Version;
            IdBox.Text = it.Id.ToString();
        }

        private void NewBlock_Click(object sender, RoutedEventArgs e)
        {
            var it = new BlockItem { Id = Guid.NewGuid(), Title = "Nový blok", Version = "1.0.0" };
            Items.Add(it);
            BlocksList.SelectedItem = it;
        }

        private void DeleteBlock_Click(object sender, RoutedEventArgs e)
        {
            var it = BlocksList.SelectedItem as BlockItem;
            if (it == null) return;
            if (MessageBox.Show($"Odstranit blok '{it.Title}'?", "Editor bloků",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Items.Remove(it);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var it = BlocksList.SelectedItem as BlockItem;
            if (it == null) return;
            it.Title = TitleBox.Text ?? "";
            it.Version = VersionBox.Text ?? "1.0.0";
            MessageBox.Show("Uloženo (ukázkově).", "Editor bloků", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }

    public sealed class BlockItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
    }
}
