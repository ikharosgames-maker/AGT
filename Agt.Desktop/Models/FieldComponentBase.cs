using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;

namespace Agt.Desktop.Models
{
    [Flags]
    public enum AnchorSides { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }

    public enum DockTo { None, Left, Top, Right, Bottom, Fill }
    public abstract class FieldComponentBase : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TypeKey { get; protected set; } = "";

        private double _x; public double X { get => _x; set { if (_x != value) { _x = value; OnPropertyChanged(); } } }
        private double _y; public double Y { get => _y; set { if (_y != value) { _y = value; OnPropertyChanged(); } } }
        private double _width = 260; public double Width { get => _width; set { if (_width != value) { _width = value; OnPropertyChanged(); } } }
        private double _height = 40; public double Height { get => _height; set { if (_height != value) { _height = value; OnPropertyChanged(); } } }
        private int _zIndex; public int ZIndex { get => _zIndex; set { if (_zIndex != value) { _zIndex = value; OnPropertyChanged(); } } }

        // Name = název komponenty (typ_blok_label_index)
        private string _name = "";
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); } } }

        // FieldKey můžeš používat pro mapování do backendu; nechávám zvlášť
        private string _fieldKey = ""; public string FieldKey { get => _fieldKey; set { if (_fieldKey != value) { _fieldKey = value; OnPropertyChanged(); } } }

        // UI popisek (u textboxu atd. zobrazený text vedle vstupu)
        private string _label = "Label"; public string Label { get => _label; set { if (_label != value) { _label = value; OnPropertyChanged(); } } }
        private bool _required; public bool Required { get => _required; set { if (_required != value) { _required = value; OnPropertyChanged(); } } }
        private string _placeholder = ""; public string Placeholder { get => _placeholder; set { if (_placeholder != value) { _placeholder = value; OnPropertyChanged(); } } }
        private string _defaultValue = ""; public string DefaultValue { get => _defaultValue; set { if (_defaultValue != value) { _defaultValue = value; OnPropertyChanged(); } } }
        private AnchorSides _anchor = AnchorSides.Left | AnchorSides.Top;
        public AnchorSides Anchor { get => _anchor; set { if (_anchor != value) { _anchor = value; OnPropertyChanged(); } } }

        private DockTo _dock = DockTo.None;
        public DockTo Dock { get => _dock; set { if (_dock != value) { _dock = value; OnPropertyChanged(); } } }

        private bool _isSelected; public bool IsSelected { get => _isSelected; set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } } }

        // Styl
        private Brush _background = Brushes.Transparent;
        public Brush Background { get => _background; set { if (_background != value) { _background = value; OnPropertyChanged(); } } }

        private Brush _foreground = Brushes.White;
        public Brush Foreground { get => _foreground; set { if (_foreground != value) { _foreground = value; OnPropertyChanged(); } } }

        private string _fontFamily = "Segoe UI";
        public string FontFamily { get => _fontFamily; set { if (_fontFamily != value) { _fontFamily = value; OnPropertyChanged(); } } }

        private double _fontSize = 14;
        public double FontSize { get => _fontSize; set { if (Math.Abs(_fontSize - value) > 0.01) { _fontSize = value; OnPropertyChanged(); } } }

        // Title použijeme pro seznam komponent vlevo
        public string Title => string.IsNullOrWhiteSpace(Name) ? $"{TypeKey} ({Label})" : Name;

        public abstract FieldComponentBase Clone();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
