using Agt.Contracts;
using Agt.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;

namespace Agt.Desktop.ViewModels;

public partial class BlocksViewModel : ObservableObject
{
    private readonly ApiClient _api;
    public ObservableCollection<BlockDefinitionDto> Blocks { get; } = new();
    [ObservableProperty] private BlockDefinitionDto? selected;
    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand NewBlockCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    public BlocksViewModel(ApiClient api)
    {
        _api = api;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        NewBlockCommand = new AsyncRelayCommand(NewBlockAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    private async Task LoadAsync()
    {
        Blocks.Clear();
        var all = await _api.GetBlocksAsync();
        foreach (var b in all.OrderBy(b => b.Id).ThenBy(b => b.Version))
            Blocks.Add(b);
        Selected = Blocks.LastOrDefault();
    }

    private Task NewBlockAsync()
    {
        Selected = new BlockDefinitionDto("block.new", 0, "Nový blok",
            new List<FieldDto> { new("title", "text", "Titulek", true) },
            new UiHintsDto("grid-1"));
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (Selected is null) return;
        // jednoduchá kontrola duplicity keys
        if (Selected.Fields.GroupBy(f => f.Key).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicity field keys.");

        var saved = await _api.SaveBlockAsync(Selected);
        // refresh
        await LoadAsync();
        Selected = saved;
    }
}
