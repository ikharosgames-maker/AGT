using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Agt.Desktop.Services;

namespace Agt.Desktop.ViewModels
{
    public sealed class DesignerViewModel : INotifyPropertyChanged
    {
        private object? _currentBlock;
        public object? CurrentBlock
        {
            get => _currentBlock;
            set { if (!ReferenceEquals(_currentBlock, value)) { _currentBlock = value; OnPropertyChanged(); } }
        }

        private string _statusText = "Připraveno";
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
        }

        private int _gridSize = 8;
        public int GridSize
        {
            get => _gridSize;
            set
            {
                var v = value < 2 ? 2 : value;
                if (_gridSize != v) { _gridSize = v; OnPropertyChanged(); }
            }
        }

        private bool _showGrid = true;
        public bool ShowGrid
        {
            get => _showGrid;
            set { if (_showGrid != value) { _showGrid = value; OnPropertyChanged(); } }
        }

        private bool _snapToGrid = true;
        public bool SnapToGridEnabled
        {
            get => _snapToGrid;
            set { if (_snapToGrid != value) { _snapToGrid = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<DesignItem> Items { get; } = new();
        public ObservableCollection<object> Elements => new ObservableCollection<object>(Items);

        public DesignerViewModel()
        {
            StatusText = "Editor bloků – připraveno";
            GridSize = 8;
            ShowGrid = true;
            SnapToGridEnabled = true;
            CurrentBlock = null;
        }

        public void NewBlock(object block)
        {
            CurrentBlock = block ?? new object();
            StatusText = "Nový blok založen.";
        }

        public Dto? ExportToDto()
        {
            return new Dto
            {
                BlockId = Guid.NewGuid(),
                Version = "1.0.0",
                Key = null,
                BlockName = null
            };
        }

        public void ImportFromDto(Dto dto)
        {
            if (dto == null) return;
            CurrentBlock = dto;
            StatusText = $"Načten blok {dto.BlockId:D} (verze {dto.Version}).";
        }

        public void AutoLayout()
        {
            StatusText = "Auto layout hotov (Phase B placeholder).";
        }

        public DesignItem CreateFromLibrary(BlockLibEntry entry, Point position)
        {
            var pos = SnapToGrid(position);
            var item = new DesignItem
            {
                Title = string.IsNullOrWhiteSpace(entry.Title) ? $"{entry.Key} ({entry.Version})" : entry.Title,
                LibraryKey = entry.Key,
                LibraryVersion = entry.Version,
                X = pos.X,
                Y = pos.Y,
                Width = 140,
                Height = 48
            };
            Items.Add(item);
            return item;
        }

        public double SnapToGrid(double v)
        {
            if (!SnapToGridEnabled || GridSize <= 1) return v;
            var g = GridSize;
            return Math.Round(v / g) * g;
        }

        public Point SnapToGrid(Point p) => new Point(SnapToGrid(p.X), SnapToGrid(p.Y));

        public sealed class Dto
        {
            public Guid BlockId { get; set; }
            public string Version { get; set; } = "1.0.0";
            public string? Key { get; set; }
            public string? BlockName { get; set; }
        }

        public sealed class DesignItem : INotifyPropertyChanged
        {
            private string _title = "";
            public string Title { get => _title; set { if (_title != value) { _title = value; OnPropertyChanged(); } } }

            public string LibraryKey { get; set; } = "";
            public string LibraryVersion { get; set; } = "1.0";

            private double _x;
            public double X { get => _x; set { if (Math.Abs(_x - value) > 0.1) { _x = value; OnPropertyChanged(); } } }

            private double _y;
            public double Y { get => _y; set { if (Math.Abs(_y - value) > 0.1) { _y = value; OnPropertyChanged(); } } }

            private double _w = 140;
            public double Width { get => _w; set { if (Math.Abs(_w - value) > 0.1) { _w = value; OnPropertyChanged(); } } }

            private double _h = 48;
            public double Height { get => _h; set { if (Math.Abs(_h - value) > 0.1) { _h = value; OnPropertyChanged(); } } }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? n = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
