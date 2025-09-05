using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Models;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels
{
    public class DesignerViewModel : ObservableObject
    {
        public ObservableCollection<FieldComponentBase> Items { get; } = new();

        private Block? _currentBlock;
        public Block? CurrentBlock { get => _currentBlock; set => SetProperty(ref _currentBlock, value); }

        // Grid
        private bool _showGrid = true;
        public bool ShowGrid { get => _showGrid; set => SetProperty(ref _showGrid, value); }

        private bool _snapToGrid = true;
        public bool SnapToGrid { get => _snapToGrid; set => SetProperty(ref _snapToGrid, value); }

        private double _gridSize = 8;
        public double GridSize { get => _gridSize; set => SetProperty(ref _gridSize, value <= 0 ? 8 : value); }

        private string _statusText = "Připraveno";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

        // Výběr
        private readonly SelectionService _selection;
        public int SelectedCount => _selection.Count;
        public FieldComponentBase? SelectedItem => _selection.SelectedItems.FirstOrDefault() as FieldComponentBase;

        // Příkazy
        public IRelayCommand AlignLeftCommand { get; }
        public IRelayCommand BringToFrontCommand { get; }
        public IRelayCommand SendToBackCommand { get; }
        public IRelayCommand DuplicateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }

        // Hromadná editace (pro PropertiesPanel Multi)
        private string? _bulkLabel; public string? BulkLabel { get => _bulkLabel; set => SetProperty(ref _bulkLabel, value); }
        private string? _bulkFieldKey; public string? BulkFieldKey { get => _bulkFieldKey; set => SetProperty(ref _bulkFieldKey, value); }
        private Brush? _bulkBackground; public Brush? BulkBackground { get => _bulkBackground; set => SetProperty(ref _bulkBackground, value); }
        private Brush? _bulkForeground; public Brush? BulkForeground { get => _bulkForeground; set => SetProperty(ref _bulkForeground, value); }
        private string? _bulkFontFamily; public string? BulkFontFamily { get => _bulkFontFamily; set => SetProperty(ref _bulkFontFamily, value); }
        private double? _bulkFontSize; public double? BulkFontSize { get => _bulkFontSize; set => SetProperty(ref _bulkFontSize, value); }
        private double? _bulkWidth; public double? BulkWidth { get => _bulkWidth; set => SetProperty(ref _bulkWidth, value); }
        private double? _bulkHeight; public double? BulkHeight { get => _bulkHeight; set => SetProperty(ref _bulkHeight, value); }

        public IRelayCommand ApplyBulkCommand { get; }

        private readonly FieldCatalogService _catalog;
        private readonly FieldFactory _factory;

        public DesignerViewModel()
        {
            _selection = (SelectionService)Application.Current.Resources["SelectionService"];
            _catalog = (FieldCatalogService)Application.Current.Resources["FieldCatalog"];
            _factory = (FieldFactory)Application.Current.Resources["FieldFactory"];

            _selection.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(SelectedItem));
            };

            AlignLeftCommand = new RelayCommand(OnAlignLeft, () => _selection.Count >= 2);
            BringToFrontCommand = new RelayCommand(OnBringToFront, HasAny);
            SendToBackCommand = new RelayCommand(OnSendToBack, HasAny);
            DuplicateCommand = new RelayCommand(OnDuplicate, HasAny);
            DeleteCommand = new AsyncRelayCommand(OnDeleteAsync, HasAny);

            ApplyBulkCommand = new RelayCommand(ApplyBulk, () => _selection.Count >= 2);
        }

        public void NewBlock(Block block)
        {
            CurrentBlock = block;
            Items.Clear();
            _selection.Clear();
            StatusText = $"Založen blok {block.Name} ({block.Id})";
        }

        private bool HasAny() => _selection.Count >= 1;

        // Vytvoření z knihovny + pojmenování typ_blok_label_index
        public void CreateFromLibrary(string key, Point position)
        {
            if (CurrentBlock == null) { StatusText = "Nejprve založte blok."; return; }

            var desc = _catalog.Items.FirstOrDefault(i => i.Key == key);
            var field = _factory.Create(key, position.X, position.Y, desc?.Defaults);

            // index pro daný typ
            var index = Items.Count(i => i.TypeKey == key) + 1;

            var blockName = Sanitize(CurrentBlock.Name);
            var labelPart = Sanitize(string.IsNullOrWhiteSpace(field.Label) ? "field" : field.Label);
            field.FieldKey = $"{key}_{blockName}_{labelPart}_{index}";

            field.ZIndex = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex) + 1;
            Items.Add(field);
            _selection.SelectSingle(field);
            StatusText = $"Vložen prvek: {desc?.DisplayName ?? key}";
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";
            var arr = s.Trim().ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var res = new string(arr);
            while (res.Contains("__")) res = res.Replace("__", "_");
            return res.Trim('_');
        }

        private void OnAlignLeft()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count < 2) return;
            var minX = selected.Min(i => i.X);
            foreach (var it in selected) it.X = minX;
            StatusText = "Zarovnáno vlevo.";
        }

        private void OnBringToFront()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var max = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex);
            foreach (var it in selected) it.ZIndex = ++max;
            StatusText = "Dopředu.";
        }

        private void OnSendToBack()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var min = Items.Count == 0 ? 0 : Items.Min(i => i.ZIndex);
            foreach (var it in selected) it.ZIndex = --min;
            StatusText = "Dozadu.";
        }

        private void OnDuplicate()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count == 0) return;
            var max = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex);
            _selection.Clear();
            foreach (var it in selected)
            {
                var clone = it.Clone();
                clone.X += GridSize; clone.Y += GridSize; clone.ZIndex = ++max;

                // Nový index a FieldKey
                var index = Items.Count(i2 => i2.TypeKey == clone.TypeKey) + 1;
                var blockName = Sanitize(CurrentBlock?.Name ?? "x");
                var labelPart = Sanitize(string.IsNullOrWhiteSpace(clone.Label) ? "field" : clone.Label);
                clone.FieldKey = $"{clone.TypeKey}_{blockName}_{labelPart}_{index}";

                Items.Add(clone);
                _selection.Toggle(clone);
            }
            StatusText = "Duplikováno.";
        }

        private Task OnDeleteAsync()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            foreach (var it in selected) Items.Remove(it);
            _selection.Clear();
            StatusText = "Smazáno.";
            return Task.CompletedTask;
        }

        // Hromadná aplikace společných vlastností
        private void ApplyBulk()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count < 2) return;

            foreach (var it in selected)
            {
                if (BulkLabel != null) it.Label = BulkLabel;
                if (BulkFieldKey != null) it.FieldKey = BulkFieldKey;
                if (BulkBackground != null) it.Background = BulkBackground;
                if (BulkForeground != null) it.Foreground = BulkForeground;
                if (BulkFontFamily != null) it.FontFamily = BulkFontFamily;
                if (BulkFontSize.HasValue) it.FontSize = BulkFontSize.Value;
                if (BulkWidth.HasValue) it.Width = BulkWidth.Value;
                if (BulkHeight.HasValue) it.Height = BulkHeight.Value;
            }

            StatusText = "Hromadná změna aplikována.";
        }

        // --- Export / Import (doplněno o nové vlastnosti) ---
        public record Dto(Guid BlockId, string BlockName, double GridSize, bool ShowGrid, bool SnapToGrid, ItemDto[] Items);
        public record ItemDto(string TypeKey, Guid Id, string FieldKey, string Label,
                              double X, double Y, double Width, double Height, int ZIndex,
                              string? DefaultValue, string Background, string Foreground, string FontFamily, double FontSize);

        private static string BrushToString(Brush b)
        {
            var scb = b as SolidColorBrush;
            return scb != null ? scb.Color.ToString() : "#00000000";
        }
        private static Brush StringToBrush(string s)
        {
            try { return (Brush)new BrushConverter().ConvertFromString(s)!; }
            catch { return Brushes.Transparent; }
        }

        public Dto ExportToDto()
        {
            var b = CurrentBlock ?? new Block { Id = Guid.Empty, Name = "" };
            return new Dto(b.Id, b.Name, GridSize, ShowGrid, SnapToGrid,
                Items.Select(i => new ItemDto(
                    i.TypeKey, i.Id, i.FieldKey, i.Label, i.X, i.Y, i.Width, i.Height, i.ZIndex, i.DefaultValue,
                    BrushToString(i.Background), BrushToString(i.Foreground), i.FontFamily, i.FontSize
                )).ToArray());
        }

        public void ImportFromDto(Dto dto)
        {
            CurrentBlock = new Block { Id = dto.BlockId, Name = dto.BlockName };
            GridSize = dto.GridSize; ShowGrid = dto.ShowGrid; SnapToGrid = dto.SnapToGrid;

            Items.Clear();
            foreach (var it in dto.Items)
            {
                var created = _factory.Create(it.TypeKey, it.X, it.Y, null);
                created.Id = it.Id;
                created.FieldKey = it.FieldKey;
                created.Label = it.Label;
                created.Width = it.Width;
                created.Height = it.Height;
                created.ZIndex = it.ZIndex;
                created.DefaultValue = it.DefaultValue ?? "";
                created.Background = StringToBrush(it.Background);
                created.Foreground = StringToBrush(it.Foreground);
                created.FontFamily = it.FontFamily;
                created.FontSize = it.FontSize;
                Items.Add(created);
            }
            StatusText = $"Načten blok {CurrentBlock.Name}";
        }

        // --- Auto layout (jednoduchý návrh) ---
        public void AutoLayout()
        {
            if (Items.Count == 0) return;
            const int columns = 2;
            double colWidth = Items.Max(i => i.Width) + GridSize * 2;
            double rowH = Items.Average(i => i.Height) + GridSize;

            int index = 0;
            foreach (var it in Items.OrderBy(i => i.Y).ThenBy(i => i.X))
            {
                int col = index % columns;
                int row = index / columns;
                it.X = col * (colWidth + GridSize);
                it.Y = row * (rowH + GridSize);
                index++;
            }
            StatusText = "Auto layout hotov.";
        }
    }
}
