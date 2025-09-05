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
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not FieldCatalogService.CatalogItem item) return;

            DragDrop.DoDragDrop(this, new DataObject("field/key", item.Key), DragDropEffects.Copy);
        }
    }
}
