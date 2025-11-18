using Agt.Desktop.Adapters;
using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Agt.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Agt.Desktop.ViewModels
{
    public class DesignerViewModel : ObservableObject
    {
        public ObservableCollection<FieldComponentBase> Items { get; } = new();

        private Block? _currentBlock;
        public Block? CurrentBlock { get => _currentBlock; set => SetProperty(ref _currentBlock, value); }

        // metadata aktuálního bloku – pro verzi a autora
        private string? _currentVersion;
        public string? CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        private string? _currentCreatedBy;
        public string? CurrentCreatedBy
        {
            get => _currentCreatedBy;
            set => SetProperty(ref _currentCreatedBy, value);
        }

        private DateTime _currentCreatedAt;
        public DateTime CurrentCreatedAt
        {
            get => _currentCreatedAt;
            set => SetProperty(ref _currentCreatedAt, value);
        }
        private bool _showGrid = true; public bool ShowGrid { get => _showGrid; set => SetProperty(ref _showGrid, value); }
        private bool _snapToGrid = true; public bool SnapToGrid { get => _snapToGrid; set => SetProperty(ref _snapToGrid, value); }
        private double _gridSize = 8; public double GridSize { get => _gridSize; set => SetProperty(ref _gridSize, value <= 0 ? 8 : value); }

        private string _statusText = "Nejprve vytvořte nebo načtěte blok.";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        private readonly SelectionService _selection;
        public int SelectedCount => _selection.Count;
        public FieldComponentBase? SelectedItem => _selection.SelectedItems.FirstOrDefault() as FieldComponentBase;

        public IRelayCommand AlignLeftCommand { get; }
        public IRelayCommand BringToFrontCommand { get; }
        public IRelayCommand SendToBackCommand { get; }
        public IRelayCommand DuplicateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand ApplyBulkCommand { get; }

        // Multi-edit vstupy
        public string? BulkName { get => _bulkName; set => SetProperty(ref _bulkName, value); }
        public string? BulkLabel { get => _bulkLabel; set => SetProperty(ref _bulkLabel, value); }
        public string? BulkFieldKey { get => _bulkFieldKey; set => SetProperty(ref _bulkFieldKey, value); }
        public Brush? BulkBackground { get => _bulkBackground; set => SetProperty(ref _bulkBackground, value); }
        public Brush? BulkForeground { get => _bulkForeground; set => SetProperty(ref _bulkForeground, value); }
        public Brush? BulkLabelForeground { get => _bulkLabelForeground; set => SetProperty(ref _bulkLabelForeground, value); }
        public Brush? BulkLabelBackground { get => _bulkLabelBackground; set => SetProperty(ref _bulkLabelBackground, value); }
        public string? BulkFontFamily { get => _bulkFontFamily; set => SetProperty(ref _bulkFontFamily, value); }
        public double? BulkFontSize { get => _bulkFontSize; set => SetProperty(ref _bulkFontSize, value); }
        public double? BulkWidth { get => _bulkWidth; set => SetProperty(ref _bulkWidth, value); }
        public double? BulkHeight { get => _bulkHeight; set => SetProperty(ref _bulkHeight, value); }

        private string? _bulkName, _bulkLabel, _bulkFieldKey, _bulkFontFamily;
        private Brush? _bulkBackground, _bulkForeground, _bulkLabelForeground, _bulkLabelBackground;
        private double? _bulkFontSize, _bulkWidth, _bulkHeight;

        private readonly FieldCatalogService _catalog;

        public DesignerViewModel()
        {
            _selection = (SelectionService)Agt.Desktop.App.Current.Resources["SelectionService"];
            _catalog = (FieldCatalogService)Agt.Desktop.App.Current.Resources["FieldCatalog"];


            _selection.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(SelectedItem));
            };

            AlignLeftCommand = new RelayCommandAdapter(OnAlignLeft, () => _selection.Count >= 2);
            BringToFrontCommand = new RelayCommandAdapter(OnBringToFront, HasAny);
            SendToBackCommand = new RelayCommandAdapter(OnSendToBack, HasAny);
            DuplicateCommand = new RelayCommandAdapter(OnDuplicate, HasAny);
            DeleteCommand = new AsyncRelayCommand(OnDeleteAsync, HasAny);
            ApplyBulkCommand = new RelayCommandAdapter(ApplyBulk, () => _selection.Count >= 2);
        }

        public void NewBlock(Block block)
        {
            CurrentBlock = block;
            Items.Clear();
            _selection.Clear();

            // nový blok – verze se určí až při prvním uložení, autor aktuální uživatel
            CurrentVersion = null;
            CurrentCreatedBy = Environment.UserName;
            CurrentCreatedAt = DateTime.UtcNow;

            StatusText = $"Založen blok {block.Name}";
        }



        private bool HasAny() => _selection.Count >= 1;

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";
            var arr = s.Trim().ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var res = new string(arr);
            while (res.Contains("__")) res = res.Replace("__", "_");
            return res.Trim('_');
        }

        // Vytvoření z knihovny + pojmenování: typ_blok_label_index
        public void CreateFromLibrary(string key, Point position)
        {
            if (CurrentBlock == null) { StatusText = "Nejprve založte blok."; return; }

            var desc = _catalog.Items.FirstOrDefault(i => i.Key == key);
            var field = FieldFactory.Create(key, position.X, position.Y, desc?.Defaults);

            var blockName = Sanitize(CurrentBlock.Name);
            var labelPart = Sanitize(string.IsNullOrWhiteSpace(field.Label) ? "field" : field.Label);
            var index = Items.Count(i => i.TypeKey == key) + 1;

            field.Name = $"{key}_{blockName}_{labelPart}_{index}";
            field.FieldKey = field.Name; // může být stejné (máš odděleně, případně později změníme)
            field.ZIndex = Items.Count == 0 ? 0 : Items.Max(i => i.ZIndex) + 1;

            Items.Add(field);
            _selection.SelectSingle(field);
            StatusText = $"Vložen prvek: {desc?.DisplayName ?? key}";
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

                var blockName = Sanitize(CurrentBlock?.Name ?? "x");
                var labelPart = Sanitize(string.IsNullOrWhiteSpace(clone.Label) ? "field" : clone.Label);
                var index = Items.Count(i2 => i2.TypeKey == clone.TypeKey) + 1;

                clone.Name = $"{clone.TypeKey}_{blockName}_{labelPart}_{index}";
                clone.FieldKey = clone.Name;

                Items.Add(clone);
                _selection.Toggle(clone);
            }
            StatusText = "Duplikováno.";
        }
        /// <summary>
        /// Export aktuálního bloku do doménového DTO (BlockDefinitionDto).
        /// </summary>
        /// <summary>
        /// Export aktuálního bloku do doménového DTO (BlockDefinitionDto).
        /// </summary>
        public BlockDefinitionDto ExportBlockDefinition(string? key = null, string? version = null)
        {
            var dto = ExportToDto();
            if (dto == null)
                throw new InvalidOperationException("ExportToDto() vrátil null.");

            var def = new BlockDefinitionDto
            {
                BlockId = dto.BlockId,
                BlockName = dto.BlockName ?? string.Empty,
                Key = key,
                // verze se bere z aktuálního stavu – Save ji pak automaticky zvýší
                Version = string.IsNullOrWhiteSpace(CurrentVersion) ? null : CurrentVersion.Trim(),
                SchemaVersion = "1.0",
                CreatedBy = string.IsNullOrWhiteSpace(CurrentCreatedBy) ? null : CurrentCreatedBy,
                CreatedAt = CurrentCreatedAt,
                GridSize = dto.GridSize,
                ShowGrid = dto.ShowGrid,
                SnapToGrid = dto.SnapToGrid
            };

            foreach (var it in dto.Items)
            {
                def.Items.Add(new FieldDefinitionDto
                {
                    TypeKey = it.TypeKey,
                    Id = it.Id,
                    Name = it.Name,
                    FieldKey = it.FieldKey,
                    Label = it.Label,
                    X = it.X,
                    Y = it.Y,
                    Width = it.Width,
                    Height = it.Height,
                    ZIndex = it.ZIndex,
                    DefaultValue = it.DefaultValue,
                    Background = it.Background,
                    Foreground = it.Foreground,
                    FontFamily = it.FontFamily,
                    FontSize = it.FontSize
                });
            }

            return def;
        }

        /// <summary>
        /// Načte blok z doménového DTO (BlockDefinitionDto) do designeru.
        /// </summary>
        /// <summary>
        /// Načte blok z doménového DTO (BlockDefinitionDto) do designeru.
        /// </summary>
        public void ImportBlockDefinition(BlockDefinitionDto definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            // metadata z DTO → do viewmodelu
            CurrentVersion = definition.Version;
            CurrentCreatedBy = definition.CreatedBy;
            CurrentCreatedAt = definition.CreatedAt;

            var items = definition.Items
                .Select(i => new ItemDto(
                    i.TypeKey,
                    i.Id,
                    i.Name,
                    i.FieldKey,
                    i.Label,
                    i.X,
                    i.Y,
                    i.Width,
                    i.Height,
                    i.ZIndex,
                    i.DefaultValue,
                    i.Background,
                    i.Foreground,
                    i.FontFamily,
                    i.FontSize
                ))
                .ToArray();

            var dto = new Dto(
                definition.BlockId,
                definition.BlockName ?? string.Empty,
                definition.GridSize,
                definition.ShowGrid,
                definition.SnapToGrid,
                items);

            ImportFromDto(dto);
            MarkSaved(definition);
        }
        // Rozlišení typu změny pro verzování
        public enum BlockChangeKind
        {
            None = 0,
            LayoutOrProperties = 1,
            Structure = 2
        }
        // Podpis struktury (Ids všech komponent z posledního uloženého/načteného stavu)
        private Guid[]? _lastSavedItemIds;
        /// <summary>
        /// Vypočítá typ změny vůči poslední uložené/načtené struktuře.
        /// Strukturou rozumíme sadu Id komponent.
        /// </summary>
        public BlockChangeKind GetChangeKind(BlockDefinitionDto current)
        {
            var currentIds = current.Items
                .Select(i => i.Id)
                .OrderBy(id => id)
                .ToArray();

            if (_lastSavedItemIds == null || _lastSavedItemIds.Length == 0)
            {
                // První uložení – bereme jako "Structure",
                // ale BumpVersion zajistí, že první verze bude 1.0.0.
                return BlockChangeKind.Structure;
            }

            if (!_lastSavedItemIds.SequenceEqual(currentIds))
                return BlockChangeKind.Structure;

            // Sada Id je stejná → změnily se jen vlastnosti/layout
            return BlockChangeKind.LayoutOrProperties;
        }

        /// <summary>
        /// Aktualizuje podpis struktury po úspěšném uložení nebo načtení z knihovny.
        /// </summary>
        public void MarkSaved(BlockDefinitionDto current)
        {
            _lastSavedItemIds = current.Items
                .Select(i => i.Id)
                .OrderBy(id => id)
                .ToArray();
        }

        public sealed record ItemDto(
    string TypeKey,
    Guid Id,
    string Name,
    string FieldKey,
    string Label,
    double X,
    double Y,
    double Width,
    double Height,
    int ZIndex,
    string? DefaultValue,
    string Background,
    string Foreground,
    string FontFamily,
    double FontSize);

        public sealed record Dto(
            Guid BlockId,
            string BlockName,
            double GridSize,
            bool ShowGrid,
            bool SnapToGrid,
            ItemDto[] Items);

        private Task OnDeleteAsync()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            foreach (var it in selected) Items.Remove(it);
            _selection.Clear();
            StatusText = "Smazáno.";
            return Task.CompletedTask;
        }

        void ApplyBulk()
        {
            var selected = _selection.SelectedItems.OfType<FieldComponentBase>().ToList();
            if (selected.Count < 2) return;

            foreach (var it in selected)
            {
                if (BulkName != null) it.Name = BulkName;
                if (BulkLabel != null) it.Label = BulkLabel;
                if (BulkFieldKey != null) it.FieldKey = BulkFieldKey;

                if (BulkBackground != null) it.Background = BulkBackground;
                if (BulkForeground != null) it.Foreground = BulkForeground;
                if (BulkLabelForeground != null) it.LabelForeground = BulkLabelForeground;
                if (BulkLabelBackground != null) it.LabelBackground = BulkLabelBackground;

                if (BulkFontFamily != null) it.FontFamily = BulkFontFamily;
                if (BulkFontSize.HasValue) it.FontSize = BulkFontSize.Value;
                if (BulkWidth.HasValue) it.Width = BulkWidth.Value;
                if (BulkHeight.HasValue) it.Height = BulkHeight.Value;
            }

            StatusText = "Hromadná změna aplikována.";
        }


        private static string BrushToString(Brush b)
        {
            if (b is SolidColorBrush scb) return scb.Color.ToString();
            return "#00000000";
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
                    i.TypeKey, i.Id, i.Name, i.FieldKey, i.Label, i.X, i.Y, i.Width, i.Height, i.ZIndex, i.DefaultValue,
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
                var created = FieldFactory.Create(it.TypeKey, it.X, it.Y, null);
                created.Id = it.Id;
                created.Name = it.Name;
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

        public void AutoLayout()
        {
            // 1) Které prvky skládat:
            //    - když je něco vybrané, skládej jen výběr
            //    - jinak všechny
            var candidates = Items.Where(i => i.IsSelected).ToList();
            if (candidates.Count == 0)
                candidates = Items.ToList();

            if (candidates.Count <= 1)
            {
                StatusText = "Auto layout: nic k uspořádání.";
                return;
            }

            // 2) Základní parametry
            int n = candidates.Count;
            double grid = GridSize > 0 ? GridSize : 8;

            // celková plocha prvků (pro odhad „čtvercové“ šířky)
            double totalArea = candidates.Sum(i =>
            {
                double w = Math.Max(1, i.Width);
                double h = Math.Max(1, i.Height);
                return w * h;
            });

            // cílová šířka řádku ~ sqrt(plocha)
            double targetRowWidth = Math.Sqrt(totalArea);
            if (double.IsNaN(targetRowWidth) || targetRowWidth <= 0)
            {
                targetRowWidth = candidates.Max(i => i.Width) + grid;
            }

            // začínáme vždy od (0,0)
            double currentX = 0;
            double currentY = 0;
            double currentRowHeight = 0;
            int rowCount = 1;

            // 3) Zachovej pořadí shora dolů, zleva doprava
            var ordered = candidates
                .OrderBy(i => i.Y)
                .ThenBy(i => i.X)
                .ToList();

            foreach (var it in ordered)
            {
                double itemWidth = Math.Max(1, it.Width);
                double itemHeight = Math.Max(1, it.Height);

                // pokud by se prvek do řádku už "nevešel", začni nový řádek
                if (currentX > 0 && currentX + itemWidth > targetRowWidth)
                {
                    currentX = 0;
                    currentY += currentRowHeight + grid;
                    currentRowHeight = 0;
                    rowCount++;
                }

                // umístění prvku
                double targetX = currentX;
                double targetY = currentY;

                // snap na mřížku
                double snappedX = Math.Round(targetX / grid) * grid;
                double snappedY = Math.Round(targetY / grid) * grid;

                it.X = snappedX;
                it.Y = snappedY;

                // posun pro další prvek v řádku
                currentX += itemWidth + grid;
                // výška řádku = max výška prvků v něm
                if (itemHeight > currentRowHeight)
                    currentRowHeight = itemHeight;
            }

            StatusText = $"Auto layout: uspořádáno {n} prvků od (0,0) do {rowCount} řádků.";
        }

    }
}
