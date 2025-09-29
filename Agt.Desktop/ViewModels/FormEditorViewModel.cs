using Agt.Domain.Models;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using System.IO;

namespace Agt.Desktop.ViewModels;

public sealed class FormEditorViewModel : ViewModelBase
{
    public ObservableCollection<BlockPaletteItem> BlockPalette { get; } = new();
    public BlockPaletteItem? SelectedPaletteItem { get; set; }

    public ObservableCollection<FormBlockItem> FormBlocks { get; } = new();
    public FormBlockItem? SelectedFormBlock { get; set; }

    public ICommand AddSelectedBlockCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand SaveDraftCommand { get; }

    public FormEditorViewModel()
    {
        AddSelectedBlockCommand = new RelayCommand(_ => AddSelected());
        MoveUpCommand = new RelayCommand(_ => Move(-1), _ => SelectedFormBlock != null);
        MoveDownCommand = new RelayCommand(_ => Move(1), _ => SelectedFormBlock != null);
        SaveDraftCommand = new RelayCommand(_ => SaveDraft());

        // TODO: načti paletu z IBlockRepository (DI – zatím můžeš naplnit mockem)
        BlockPalette.Add(new BlockPaletteItem("QC_Input", "Vstupní kontrola", "1.0.0"));
        BlockPalette.Add(new BlockPaletteItem("VisualCheck", "Vizuální kontrola", "2.1.0"));
    }

    void AddSelected()
    {
        if (SelectedPaletteItem is null) return;
        FormBlocks.Add(new FormBlockItem
        {
            Key = SelectedPaletteItem.Key,
            Version = SelectedPaletteItem.Version,
            Title = SelectedPaletteItem.Name
        });
    }

    void Move(int delta)
    {
        var idx = FormBlocks.IndexOf(SelectedFormBlock!);
        var newIdx = Math.Clamp(idx + delta, 0, FormBlocks.Count - 1);
        if (newIdx == idx) return;
        FormBlocks.RemoveAt(idx);
        FormBlocks.Insert(newIdx, SelectedFormBlock!);
    }

    void SaveDraft()
    {
        // převeď sestavu na pins json (key+version v pořadí)
        var pins = FormBlocks.Select(b => new { key = b.Key, version = b.Version }).ToList();
        var json = JsonSerializer.Serialize(pins, new JsonSerializerOptions { WriteIndented = true });

        // TODO: zapiš do vybrané FormVersion.BlockPinsJson + UpsertVersion přes IFormRepository
        // (zatím jen uložíme do souboru pro snadnou kontrolu)
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "last_form_pins.json");
        File.WriteAllText(path, json);
    }
}

public sealed class BlockPaletteItem(string key, string name, string version)
{
    public string Key { get; } = key;
    public string Name { get; } = name;
    public string Version { get; } = version;
}

public sealed class FormBlockItem
{
    public string Key { get; set; } = "";
    public string Version { get; set; } = "";
    public string Title { get; set; } = "";
}
