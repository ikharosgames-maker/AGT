using Agt.Desktop.Services;
using System.Text.Json.Nodes;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class FormRepositoryBrowserWindow : Window
    {
        public JsonNode? SelectedFormJson { get; private set; }
        public string? SelectedFormKey { get; private set; }

        public FormRepositoryBrowserWindow()
        {
            InitializeComponent();
            var vm = new FormRepositoryBrowserViewModel(new FormsFolderCatalogService());
            vm.OnOpen += Vm_OnOpen;
            DataContext = vm;
            vm.Refresh();
        }

        private void Vm_OnOpen(JsonNode? json, string? key)
        {
            SelectedFormJson = json;
            SelectedFormKey = key;
            DialogResult = json != null;
            Close();
        }
    }
}
