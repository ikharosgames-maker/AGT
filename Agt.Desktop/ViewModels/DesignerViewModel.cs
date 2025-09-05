using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels;

public enum ControlType { Text, TextArea, Combo, Check, Date, Label, ListView, DataGrid }

public class ToolboxItem
{
    public string Display { get; init; } = "";
    public ControlType Type { get; init; }
}

public class BlockControl : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public Guid Id { get; set; } = Guid.NewGuid();

    private ControlType controlType = ControlType.Text;
    public ControlType ControlType { get => controlType; set { controlType = value; UpdateCapabilities(); OnChanged(); } }

    // Umístění a rozměry (kompaktní defaulty)
    private double x; public double X { get => x; set { x = value; OnChanged(); } }
    private double y; public double Y { get => y; set { y = value; OnChanged(); } }
    private double w = 160; public double Width { get => w; set { w = Math.Max(40, value); OnChanged(); } }
    private double h = 28; public double Height { get => h; set { h = Math.Max(24, value); OnChanged(); } }

    // UI vlastnosti
    private string label = "Label"; public string Label { get => label; set { label = value; OnChanged(); } }
    private string dataPath = ""; public string DataPath { get => dataPath; set { dataPath = value; OnChanged(); } }
    private string placeholder = ""; public string Placeholder { get => placeholder; set { placeholder = value; OnChanged(); } } // hint text
    private bool ro; public bool IsReadOnly { get => ro; set { ro = value; OnChanged(); } }
    private bool ml; public bool IsMultiline { get => ml; set { ml = value; OnChanged(); } }

    private string fgHex = "#EEEEEE"; public string ForegroundHex { get => fgHex; set { fgHex = value; OnChanged(); OnChanged(nameof(ForegroundBrush)); } }
    private string bgHex = "#2A2A2A"; public string BackgroundHex { get => bgHex; set { bgHex = value; OnChanged(); OnChanged(nameof(BackgroundBrush)); } }
    private double fontSize = 13; public double FontSize { get => fontSize; set { fontSize = value; OnChanged(); } }

    private string? selectedItem; public string? SelectedItem { get => selectedItem; set { selectedItem = value; OnChanged(); } }
    private List<string> items = new() { "Item 1", "Item 2", "Item 3" }; public List<string> Items { get => items; set { items = value; OnChanged(); } }

    [JsonIgnore] public Brush ForegroundBrush => (Brush)new BrushConverter().ConvertFromString(ForegroundHex)!;
    [JsonIgnore] public Brush BackgroundBrush => (Brush)new BrushConverter().ConvertFromString(BackgroundHex)!;

    private bool isSelected; public bool IsSelected { get => isSelected; set { isSelected = value; OnChanged(); } }
    public System.Windows.HorizontalAlignment HAlign { get; set; } = System.Windows.HorizontalAlignment.Left;

    // Viditelnost vlastností dle typu
    private bool showMultiline, showItems, showBackground, showReadOnly = true;
    public bool ShowMultiline { get => showMultiline; private set { showMultiline = value; OnChanged(); } }
    public bool ShowItems { get => showItems; private set { showItems = value; OnChanged(); } }
    public bool ShowBackground { get => showBackground; private set { showBackground = value; OnChanged(); } }
    public bool ShowReadOnly { get => showReadOnly; private set { showReadOnly = value; OnChanged(); } }

    private void UpdateCapabilities()
    {
        ShowMultiline = ControlType is ControlType.Text or ControlType.TextArea;
        ShowItems = ControlType is ControlType.Combo or ControlType.ListView or ControlType.DataGrid;
        ShowBackground = ControlType != ControlType.Label;
        ShowReadOnly = ControlType is not ControlType.Label;

        (Width, Height) = ControlType switch
        {
            ControlType.Label => (100, 24),
            ControlType.Text => (160, 28),
            ControlType.TextArea => (220, 80),
            ControlType.Combo => (160, 28),
            ControlType.Check => (140, 24),
            ControlType.Date => (160, 28),
            ControlType.ListView => (300, 140),
            ControlType.DataGrid => (360, 160),
            _ => (160, 28)
        };
    }
}

/* Společné schopnosti pro multi-select panel */
public record CommonCapabilities(
    bool ShowLabel, bool ShowDataPath, bool ShowPlaceholder, bool ShowReadOnly,
    bool ShowMultiline, bool ShowHAlign, bool ShowFont, bool ShowForeground, bool ShowBackground);

/* DTO pro lokální design uložený na disk */
public record DesignDto(string BlockId, string BlockName, List<BlockControl> Controls);

/* DTO pro runtime export (kompatibilní styl) */
public class RuntimeFieldDto
{
    public string Key { get; set; } = "";
    public string Type { get; set; } = "text";     // "text","number","date","bool","list"...
    public string Label { get; set; } = "";
    public bool Required { get; set; } = false;
}

public class RuntimeUiElementDto
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";         // "TextBox","Label",...
    public string? Key { get; set; }               // null pro ne-input prvky (Label)
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    // základní styling
    public string? Foreground { get; set; }
    public string? Background { get; set; }
    public double? FontSize { get; set; }
}

public class RuntimeUiHintsDto
{
    public string Layout { get; set; } = "absolute";
    public List<RuntimeUiElementDto> Elements { get; set; } = new();
}

public class RuntimeBlockDefinitionDto
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public List<RuntimeFieldDto> Fields { get; set; } = new();
    public RuntimeUiHintsDto UiHints { get; set; } = new();
}

public partial class DesignerViewModel : ObservableObject
{
    private readonly ApiClient _api;
    public FormViewModel FormVM { get; }

    public ObservableCollection<ToolboxItem> Toolbox { get; } = new();
    public ObservableCollection<BlockControl> Controls { get; } = new();

    private ToolboxItem? selectedToolboxItem;
    public ToolboxItem? SelectedToolboxItem { get => selectedToolboxItem; set => SetProperty(ref selectedToolboxItem, value); }

    private BlockControl? selected;
    public BlockControl? Selected
    {
        get => selected;
        set
        {
            if (selected != null) selected.IsSelected = false;
            SetProperty(ref selected, value);
            if (selected != null) selected.IsSelected = true;
            OnPropertyChanged(nameof(Selected));
            RefreshCommonCaps();
        }
    }

    private string? status;
    public string? Status { get => status; set => SetProperty(ref status, value); }

    // Plátno (pro clamp)
    public double CanvasWidth { get; set; } = 800;
    public double CanvasHeight { get; set; } = 600;

    // Identita bloku (EXPPLICITNÍ vlastnosti – bez source generátoru)
    private string blockId = "block.new";
    public string BlockId
    {
        get => blockId;
        set => SetProperty(ref blockId, value);
    }

    private string blockName = "Nový blok";
    public string BlockName
    {
        get => blockName;
        set => SetProperty(ref blockName, value);
    }

    // Commands
    public IAsyncRelayCommand NewBlockCommand { get; }
    public IAsyncRelayCommand SaveBlockLocalCommand { get; }
    public IAsyncRelayCommand LoadBlockLocalCommand { get; }
    public IAsyncRelayCommand ExportRuntimeCommand { get; }
    public IRelayCommand DeleteSelectedCommand { get; }

    // Multi-select stav
    public CommonCapabilities CommonCaps { get; private set; } =
        new(true, true, true, true, true, true, true, true, true);

    public bool MultiSelectionVisible => Controls.Count(c => c.IsSelected) > 1;
    public string MultiSelectionTitle => $"{Controls.Count(c => c.IsSelected)} prvků vybráno";

    public DesignerViewModel(ApiClient api)
    {
        _api = api;
        FormVM = new FormViewModel(api);

        Toolbox.Add(new ToolboxItem { Display = "TextBox", Type = ControlType.Text });
        Toolbox.Add(new ToolboxItem { Display = "TextArea", Type = ControlType.TextArea });
        Toolbox.Add(new ToolboxItem { Display = "ComboBox", Type = ControlType.Combo });
        Toolbox.Add(new ToolboxItem { Display = "CheckBox", Type = ControlType.Check });
        Toolbox.Add(new ToolboxItem { Display = "DatePicker", Type = ControlType.Date });
        Toolbox.Add(new ToolboxItem { Display = "Label", Type = ControlType.Label });
        Toolbox.Add(new ToolboxItem { Display = "ListView", Type = ControlType.ListView });
        Toolbox.Add(new ToolboxItem { Display = "DataGrid", Type = ControlType.DataGrid });

        NewBlockCommand = new AsyncRelayCommand(NewBlockAsync);
        SaveBlockLocalCommand = new AsyncRelayCommand(SaveBlockLocalAsync);
        LoadBlockLocalCommand = new AsyncRelayCommand(LoadBlockLocalAsync);
        ExportRuntimeCommand = new AsyncRelayCommand(ExportRuntimeAsync);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => Controls.Any(c => c.IsSelected));
    }

    /* ---------- Multi-select společné vlastnosti ---------- */
    public void RefreshCommonCaps()
    {
        var sel = Controls.Where(c => c.IsSelected).ToList();
        if (sel.Count <= 1)
        {
            CommonCaps = new(
                ShowLabel: true, ShowDataPath: true, ShowPlaceholder: true, ShowReadOnly: true,
                ShowMultiline: true, ShowHAlign: true, ShowFont: true, ShowForeground: true, ShowBackground: true);
        }
        else
        {
            bool All(Func<BlockControl, bool> prop) => sel.All(prop);
            CommonCaps = new(
                ShowLabel: true, // každý prvek má Label
                ShowDataPath: true, // necháváme viditelné
                ShowPlaceholder: All(c => c.ControlType is ControlType.Text or ControlType.TextArea),
                ShowReadOnly: All(c => c.ControlType is not ControlType.Label),
                ShowMultiline: All(c => c.ControlType is ControlType.Text or ControlType.TextArea),
                ShowHAlign: true,
                ShowFont: true,
                ShowForeground: true,
                ShowBackground: All(c => c.ControlType != ControlType.Label));
        }
        OnPropertyChanged(nameof(CommonCaps));
        OnPropertyChanged(nameof(MultiSelectionVisible));
        OnPropertyChanged(nameof(MultiSelectionTitle));
    }

    /* ---------- Přidávání / pohyb / resize se snapem a clampem ---------- */
    private static double Snap(double v, int g = 8) => Math.Round(v / g) * g;
    private double ClampX(double x, double w) => Math.Max(0, Math.Min(x, Math.Max(0, CanvasWidth - w)));
    private double ClampY(double y, double h) => Math.Max(0, Math.Min(y, Math.Max(0, CanvasHeight - h)));

    public void AddControlAt(double x, double y)
    {
        if (SelectedToolboxItem is null) return;
        var c = new BlockControl { ControlType = SelectedToolboxItem.Type };
        c.X = ClampX(Snap(x), c.Width);
        c.Y = ClampY(Snap(y), c.Height);

        // unikátní label v rámci bloku
        var baseName = $"{c.ControlType.ToString().ToLower()}_{BlockId.Replace('.', '_')}";
        var index = 1;
        var names = Controls.Select(cc => cc.Label).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidate = $"{baseName}_{index}";
        while (names.Contains(candidate)) candidate = $"{baseName}_{++index}";
        c.Label = candidate;

        Controls.Add(c);

        foreach (var it in Controls) it.IsSelected = false;
        c.IsSelected = true;
        Selected = c;
        Status = $"Přidán {SelectedToolboxItem.Display}.";

        SelectedToolboxItem = null; // po vložení zruš výběr nástroje
        RefreshCommonCaps();
    }

    public void MoveSelectedBy(double dx, double dy)
    {
        var sel = Controls.Where(c => c.IsSelected).ToList();
        foreach (var c in sel)
        {
            c.X = ClampX(Snap(c.X + dx), c.Width);
            c.Y = ClampY(Snap(c.Y + dy), c.Height);
        }
    }

    public void ResizeSelected(double dW, double dH, double dX, double dY)
    {
        var c = Selected;
        if (c is null) return;
        var newW = Snap(c.Width + dW);
        var newH = Snap(c.Height + dH);
        var newX = Snap(c.X + dX);
        var newY = Snap(c.Y + dY);
        c.Width = Math.Max(40, Math.Min(newW, CanvasWidth));
        c.Height = Math.Max(24, Math.Min(newH, CanvasHeight));
        c.X = ClampX(newX, c.Width);
        c.Y = ClampY(newY, c.Height);
    }

    private void DeleteSelected()
    {
        var selected = Controls.Where(c => c.IsSelected).ToList();
        foreach (var c in selected) Controls.Remove(c);
        Selected = Controls.FirstOrDefault();
        RefreshCommonCaps();
    }

    /* ---------- Jedinečnost BlockId ---------- */
    private string EnsureUniqueBlockId(string candidate)
    {
        var safe = (candidate ?? "").Trim();
        if (string.IsNullOrWhiteSpace(safe)) safe = "block.new";
        var folder = DesignDir;
        var path = Path.Combine(folder, $"{safe}.json");
        if (!File.Exists(path)) return safe;

        int i = 1;
        while (File.Exists(Path.Combine(folder, $"{safe}-{i}.json"))) i++;
        return $"{safe}-{i}";
    }

    /* ---------- Lokální SAVE/LOAD (AppData) ---------- */
    private string DesignDir
    {
        get
        {
            var d = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "BlockDesigns");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    private async Task SaveBlockLocalAsync()
    {
        if (string.IsNullOrWhiteSpace(BlockId))
        {
            Status = "Zadej Block Id před uložením.";
            return;
        }
        BlockId = EnsureUniqueBlockId(BlockId); // zajistí unikátní soubor
        var dto = new DesignDto(BlockId, BlockName, Controls.ToList());
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(DesignDir, $"{BlockId}.json");
        await File.WriteAllTextAsync(path, json);
        Status = $"Uloženo: {path}";
    }

    private async Task LoadBlockLocalAsync()
    {
        if (string.IsNullOrWhiteSpace(BlockId))
        {
            Status = "Zadej Block Id pro načtení.";
            return;
        }
        var path = Path.Combine(DesignDir, $"{BlockId}.json");
        if (!File.Exists(path))
        {
            Status = "Soubor nenalezen.";
            return;
        }
        var json = await File.ReadAllTextAsync(path);
        var dto = JsonSerializer.Deserialize<DesignDto>(json);
        if (dto is null) { Status = "Načtení selhalo."; return; }

        BlockName = dto.BlockName;
        Controls.Clear();
        foreach (var c in dto.Controls)
        {
            // udrž v aktuálních hranicích plátna
            c.X = ClampX(c.X, c.Width);
            c.Y = ClampY(c.Y, c.Height);
            Controls.Add(c);
        }
        Selected = Controls.FirstOrDefault();
        RefreshCommonCaps();
        Status = $"Načteno {dto.Controls.Count} prvků.";
    }

    private async Task NewBlockAsync()
    {
        if (string.IsNullOrWhiteSpace(BlockId))
        {
            Status = "Zadej Block Id (např. block.customerHeader).";
            return;
        }
        BlockId = EnsureUniqueBlockId(BlockId);
        Controls.Clear();
        Status = $"Nový blok: {BlockId}";
        await Task.CompletedTask;
    }

    /* ---------- Export: DESIGN → RUNTIME (absolute uiHints) ---------- */
    private RuntimeBlockDefinitionDto CompileToRuntime()
    {
        var rt = new RuntimeBlockDefinitionDto
        {
            Id = BlockId,
            Version = 1,
            Name = BlockName
        };

        // 1) pole pro inputy
        foreach (var c in Controls)
        {
            if (c.ControlType is ControlType.Label or ControlType.ListView or ControlType.DataGrid)
                continue;

            var key = string.IsNullOrWhiteSpace(c.DataPath)
                ? c.Label.Replace(' ', '_').ToLowerInvariant()
                : c.DataPath;

            var type = c.ControlType switch
            {
                ControlType.Text or ControlType.TextArea => "text",
                ControlType.Check => "bool",
                ControlType.Date => "date",
                ControlType.Combo => "text",
                _ => "text"
            };

            rt.Fields.Add(new RuntimeFieldDto
            {
                Key = key,
                Type = type,
                Label = c.Label,
                Required = false
            });
        }

        // 2) uiHints s absolutními souřadnicemi
        foreach (var c in Controls)
        {
            string? key = null;
            if (c.ControlType is not ControlType.Label and not ControlType.ListView and not ControlType.DataGrid)
            {
                key = string.IsNullOrWhiteSpace(c.DataPath)
                    ? c.Label.Replace(' ', '_').ToLowerInvariant()
                    : c.DataPath;
            }

            rt.UiHints.Elements.Add(new RuntimeUiElementDto
            {
                Id = c.Id.ToString("N"),
                Kind = c.ControlType.ToString(),
                Key = key,
                X = c.X,
                Y = c.Y,
                W = c.Width,
                H = c.Height,
                Foreground = c.ForegroundHex,
                Background = c.BackgroundHex,
                FontSize = c.FontSize
            });
        }
        return rt;
    }

    private string RuntimeDir
    {
        get
        {
            var d = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "RuntimeBlocks");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    private async Task ExportRuntimeAsync()
    {
        if (string.IsNullOrWhiteSpace(BlockId))
        {
            Status = "Zadej Block Id před exportem.";
            return;
        }
        var rt = CompileToRuntime();
        var json = JsonSerializer.Serialize(rt, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(RuntimeDir, $"{BlockId}.json");
        await File.WriteAllTextAsync(path, json);
        Status = $"Exportováno do runtime: {path}";
    }
}
