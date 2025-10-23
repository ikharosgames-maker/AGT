using Agt.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Input;

namespace Agt.Desktop.Views
{
    public sealed class FormRepositoryBrowserViewModel
    {
        private readonly IFormCatalogService _catalog;
        public ObservableCollection<FormCatalogItem> Items { get; } = new();
        public FormCatalogItem? Selected { get; set; }
        public string Filter { get; set; } = "";
        public string Status { get; set; } = "";

        public event Action<JsonNode?, string?>? OnOpen;

        public ICommand RefreshCommand { get; }
        public ICommand OpenCommand { get; }

        public FormRepositoryBrowserViewModel(IFormCatalogService catalog)
        {
            _catalog = catalog;
            RefreshCommand = new RelayCommand(_ => Refresh());
            OpenCommand = new RelayCommand(_ => OpenSelected(), _ => Selected != null);
        }

        public void Refresh()
        {
            Items.Clear();
            var all = _catalog.Enumerate()
                              .OrderByDescending(i => i.ModifiedUtc)
                              .ToList();
            foreach (var it in all)
                if (PassFilter(it)) Items.Add(it);

            Status = $"{Items.Count} položek";
        }

        private bool PassFilter(FormCatalogItem it)
        {
            if (string.IsNullOrWhiteSpace(Filter)) return true;
            var f = Filter.Trim();
            return (it.Key?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (it.Version?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (it.Title?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private void OpenSelected()
        {
            if (Selected == null) return;
            var json = _catalog.LoadJson(Selected.FilePath, out var key);
            OnOpen?.Invoke(json, key);
        }

        private sealed class RelayCommand : ICommand
        {
            private readonly Action<object?> _exec;
            private readonly Predicate<object?>? _can;
            public RelayCommand(Action<object?> exec, Predicate<object?>? can = null) { _exec = exec; _can = can; }
            public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _exec(parameter);
            public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
        }
    }
}
