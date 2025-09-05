using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using Agt.Desktop.Models;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels
{
    public class DesignerViewModel
    {
        public ObservableCollection<FieldComponentBase> Items { get; } = new();

        public IRelayCommand GroupCommand { get; }
        public IRelayCommand AlignLeftCommand { get; }
        public IRelayCommand BringToFrontCommand { get; }
        public IRelayCommand SendToBackCommand { get; }
        public IRelayCommand DuplicateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }

        public string StatusText { get; set; } = "Připraveno";

        private readonly SelectionService _selection;
        private readonly FieldCatalogService _catalog;
        private readonly FieldFactory _factory;

        public DesignerViewModel()
        {
            _selection = (SelectionService)Application.Current.Resources["SelectionService"];
            _catalog = (FieldCatalogService)Application.Current.Resources["FieldCatalog"];
            _factory = (FieldFactory)Application.Current.Resources["FieldFactory"];

            GroupCommand = new RelayCommand(OnGroup, () => _selection.Count >= 2);
            AlignLeftCommand = new RelayCommand(OnAlignLeft, () => _selection.Count >= 2);
            BringToFrontCommand = new RelayCommand(OnBringToFront, HasAny);
            SendToBackCommand = new RelayCommand(OnSendToBack, HasAny);
            DuplicateCommand = new RelayCommand(OnDuplicate, HasAny);
            DeleteCommand = new AsyncRelayCommand(OnDeleteAsync, HasAny);
        }

        private bool HasAny() => _selection.Count >= 1;

        public void CreateFromLibrary(string key, Point position)
        {
            var desc = _catalog.Items.FirstOrDefault(i => i.Key == key);
            var field = _factory.Create(key, position.X, position.Y, desc?.Defaults);
            field.ZIndex = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex) + 1;
            Items.Add(field);
            _selection.SelectSingle(field);
            StatusText = $"Vložen prvek: {desc?.DisplayName ?? key}";
        }

        private void OnGroup() => StatusText = "Seskupení: TODO (připravím v další vlně)";

        private void OnAlignLeft()
        {
            var selected = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (selected.Count < 2) return;
            var minX = selected.Min(i => i.X);
            foreach (var it in selected) it.X = minX;
            StatusText = "Zarovnáno vlevo.";
        }

        private void OnBringToFront()
        {
            var selected = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var max = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex);
            foreach (var it in selected) it.ZIndex = ++max;
            StatusText = "Přenesení dopředu.";
        }

        private void OnSendToBack()
        {
            var selected = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var min = Items.Count == 0 ? 0 : Items.Min(i => i.ZIndex);
            foreach (var it in selected) it.ZIndex = --min;
            StatusText = "Přenesení dozadu.";
        }

        private void OnDuplicate()
        {
            var selected = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var max = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex);
            _selection.Clear();
            foreach (var it in selected)
            {
                var clone = it.Clone();
                clone.X += 10; clone.Y += 10; clone.ZIndex = ++max;
                Items.Add(clone);
                _selection.Toggle(clone);
            }
            StatusText = "Duplikováno.";
        }

        private Task OnDeleteAsync()
        {
            var selected = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            foreach (var it in selected) Items.Remove(it);
            _selection.Clear();
            StatusText = "Smazáno.";
            return Task.CompletedTask;
        }
    }
}
