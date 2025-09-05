using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Agt.Contracts;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels;

public partial class FormViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public FormDefinitionDto Definition { get; private set; }
    public ObservableCollection<FieldInstance> Fields { get; } = new();

    public IAsyncRelayCommand LoadAsyncCommand { get; }
    public IAsyncRelayCommand SaveAsyncCommand { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? statusMessage;

    public FormViewModel(ApiClient api)
    {
        _api = api;
        Definition = new FormDefinitionDto("form.claim", 2, "Reklamace", Array.Empty<FormBlockRefDto>());

        // Explicitní příkazy (bez [RelayCommand] atributu)
        LoadAsyncCommand = new AsyncRelayCommand(LoadAsync);
        SaveAsyncCommand = new AsyncRelayCommand(SaveAsync);
    }
    public async Task<bool> TryLoadAsync()
    {
        try { await LoadAsync(); return true; }
        catch { return false; } // tichý fallback v MVP
    }
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var (form, blocks) = await _api.ResolveFormAsync("form.claim", 2);
            Definition = form;
            Fields.Clear();
            foreach (var b in form.Blocks)
            {
                var block = blocks.First(x => x.Id == b.Ref && x.Version == b.Version);
                var blockKey = block.Id.Split('.').Last();
                foreach (var f in block.Fields)
                    Fields.Add(new FieldInstance(blockKey, f));
            }
            OnPropertyChanged(nameof(Definition));
            StatusMessage = "Načteno.";
        }
        finally { IsBusy = false; }
    }

    public async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            var grouped = Fields
                .GroupBy(f => f.BlockKey)
                .ToDictionary(g => g.Key, g => g.ToDictionary(
                    x => x.Field.Key, x => x.Value));

            using var doc = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(grouped));
            var root = doc.RootElement.Clone();

            var created = await _api.CreateResponseAsync(new FormResponseCreateDto(
                FormId: Definition.Id,
                FormVersion: Definition.Version,
                Data: root,
                CreatedBy: Environment.UserName));

            StatusMessage = created is null ? "Uložení selhalo" : $"Uloženo (Id: {created.Id}).";
        }
        finally { IsBusy = false; }
    }
}
