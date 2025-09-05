using System.Collections.ObjectModel;

namespace Agt.Desktop.Services
{
    public class SelectionService
    {
        public ObservableCollection<object> SelectedItems { get; } = new();
        public int Count => SelectedItems.Count;

        public bool IsSelected(object? item) => item != null && SelectedItems.Contains(item);

        public void Clear() => SelectedItems.Clear();

        public void SelectSingle(object? item)
        {
            SelectedItems.Clear();
            if (item != null) SelectedItems.Add(item);
        }

        public void Toggle(object? item)
        {
            if (item == null) return;
            if (SelectedItems.Contains(item)) SelectedItems.Remove(item);
            else SelectedItems.Add(item);
        }
    }
}
