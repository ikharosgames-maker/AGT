using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Agt.Desktop.Models;
using Agt.Desktop.Services;

namespace Agt.Desktop.Views
{
    public partial class ComponentsListView : UserControl
    {
        private SelectionService _selection => (SelectionService)Application.Current.Resources["SelectionService"];

        public ComponentsListView()
        {
            InitializeComponent();
            List.SelectionChanged += List_SelectionChanged;
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // synchronizace se SelectionService
            foreach (var rem in e.RemovedItems.OfType<FieldComponentBase>())
                if (_selection.IsSelected(rem)) _selection.Toggle(rem);

            foreach (var add in e.AddedItems.OfType<FieldComponentBase>())
                if (!_selection.IsSelected(add)) _selection.Toggle(add);
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // budoucí „najeď na položku“ – zatím nic
        }
    }
}
