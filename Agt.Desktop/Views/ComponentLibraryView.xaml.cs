using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Agt.Desktop.Services;

namespace Agt.Desktop.Views
{
    public partial class ComponentLibraryView : UserControl
    {
        private FieldCatalogService Catalog => (FieldCatalogService)Application.Current.Resources["FieldCatalog"];

        public ComponentLibraryView()
        {
            InitializeComponent();
            List.ItemsSource = Catalog.Items;
            List.PreviewMouseMove += List_PreviewMouseMove;
        }

        private void List_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (List.SelectedItem is not FieldCatalogService.FieldDescriptor it) return;

            var data = new DataObject();
            data.SetData("field/key", it.Key);
            DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
        }
    }
}
