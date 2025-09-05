using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public class SelectionService : INotifyPropertyChanged
    {
        public ObservableCollection<object> SelectedItems { get; } = new();

        public SelectionService()
        {
            SelectedItems.CollectionChanged += (_, __) => OnPropertyChanged(nameof(Count));
        }

        public int Count => SelectedItems.Count;

        public bool IsSelected(object? item) => item != null && SelectedItems.Contains(item);

        public void Clear()
        {
            foreach (var i in SelectedItems)
                if (i is FieldComponentBase f) f.IsSelected = false;
            SelectedItems.Clear();
        }

        public void SelectSingle(object? item)
        {
            Clear();
            if (item != null)
            {
                SelectedItems.Add(item);
                if (item is FieldComponentBase f) f.IsSelected = true;
            }
        }

        public void Toggle(object? item)
        {
            if (item == null) return;
            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
                if (item is FieldComponentBase f) f.IsSelected = false;
            }
            else
            {
                SelectedItems.Add(item);
                if (item is FieldComponentBase f) f.IsSelected = true;
            }
            OnPropertyChanged(nameof(Count));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
