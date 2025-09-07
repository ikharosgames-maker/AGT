using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Agt.Desktop.Services;

namespace Agt.Desktop.Views
{
    public partial class ComponentLibraryView : UserControl
    {
        public ComponentLibraryView()
        {
            InitializeComponent();
            Loaded += OnLoaded;  // katalog načteme až po načtení vizuálního stromu
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Zkuste najít katalog ve zdrojích – nejdřív lokálně, pak v App
            var catalog =
                this.TryFindResource("FieldCatalog") as FieldCatalogService
                ?? Application.Current?.Resources["FieldCatalog"] as FieldCatalogService;

            if (catalog == null)
            {
                // Bez pádu: ukaž placeholder
                List.ItemsSource = new ObservableCollection<dynamic>
                {
                    new { DisplayName = "Katalog komponent nenalezen (FieldCatalog)", Key = "" }
                };
                List.IsEnabled = false;
                return;
            }

            List.IsEnabled = true;
            List.ItemsSource = catalog.Items;
            List.DisplayMemberPath = "DisplayName";

            // Drag & drop jen když máme skutečná data
            List.PreviewMouseMove -= List_PreviewMouseMove;
            List.PreviewMouseMove += List_PreviewMouseMove;
        }

        private void List_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!List.IsEnabled) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (List.SelectedItem is not FieldCatalogService.FieldDescriptor it) return;

            var data = new DataObject();
            data.SetData("field/key", it.Key);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
        }
    }
}
