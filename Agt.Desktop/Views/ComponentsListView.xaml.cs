using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Services;

namespace Agt.Desktop.Views
{
    public partial class ComponentsListView : UserControl
    {
        private SelectionService Selection => (SelectionService)Application.Current.Resources["SelectionService"];

        public ComponentsListView()
        {
            InitializeComponent();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // synchronizace výběru se SelectionService
            var selected = List.SelectedItems.Cast<object>().ToHashSet();
            Selection.ReplaceWith(selected);
        }
    }
}
