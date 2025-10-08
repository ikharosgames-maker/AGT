using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Agt.Desktop.Models
{
    [Flags]
    public enum AnchorSides { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }
    public enum DockTo { None, Left, Top, Right, Bottom, Fill }

    public abstract class FieldComponentBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private double _totalWidth;
        public double TotalWidth
        {
            get => _totalWidth;
            set { if (_totalWidth != value) { _totalWidth = value; OnPropertyChanged(nameof(TotalWidth)); } }
        }

        private double _totalHeight;
        public double TotalHeight
        {
            get => _totalHeight;
            set { if (_totalHeight != value) { _totalHeight = value; OnPropertyChanged(nameof(TotalHeight)); } }
        }

        // doporučuji v ctoru nebo při tvorbě komponent nastavit default
        protected FieldComponentBase()
        {
            TotalWidth = Width;   // pokud někdo nenastaví, padne to na skutečné Width/Height
            TotalHeight = Height;
        }
        // ---------- Identita ----------
        private Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _fieldKey = "";
        public string FieldKey
        {
            get => _fieldKey;
            set { if (_fieldKey != value) { _fieldKey = value; OnPropertyChanged(); } }
        }

        private string _typeKey = "";
        public string TypeKey
        {
            get => _typeKey;
            set { if (_typeKey != value) { _typeKey = value; OnPropertyChanged(); } }
        }

        // ---------- Stav výběru ----------
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        // ---------- UI popisky ----------
        private string _label = "Label";
        public string Label
        {
            get => _label;
            set { if (_label != value) { _label = value; OnPropertyChanged(); } }
        }

        private string _placeholder = "";
        public string Placeholder
        {
            get => _placeholder;
            set { if (_placeholder != value) { _placeholder = value; OnPropertyChanged(); } }
        }

        private bool _required;
        public bool Required
        {
            get => _required;
            set { if (_required != value) { _required = value; OnPropertyChanged(); } }
        }

        private string? _defaultValue;
        public string? DefaultValue
        {
            get => _defaultValue;
            set { if (_defaultValue != value) { _defaultValue = value; OnPropertyChanged(); } }
        }

        // ---------- Pozice a velikost ----------
        private double _x;
        public double X
        {
            get => _x;
            set { if (Math.Abs(_x - value) > double.Epsilon) { _x = value; OnPropertyChanged(); } }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set { if (Math.Abs(_y - value) > double.Epsilon) { _y = value; OnPropertyChanged(); } }
        }

        private double _width = 200;
        public double Width
        {
            get => _width;
            set { if (Math.Abs(_width - value) > double.Epsilon) { _width = value; OnPropertyChanged(); } }
        }

        private double _height = 28;
        public double Height
        {
            get => _height;
            set { if (Math.Abs(_height - value) > double.Epsilon) { _height = value; OnPropertyChanged(); } }
        }

        private int _zIndex;
        public int ZIndex
        {
            get => _zIndex;
            set { if (_zIndex != value) { _zIndex = value; OnPropertyChanged(); } }
        }

        // ---------- Vzhled ----------
        private Brush _background = Brushes.Transparent;
        public Brush Background
        {
            get => _background;
            set { if (_background != value) { _background = value; OnPropertyChanged(); } }
        }

        private Brush _foreground = Brushes.Transparent;
        public Brush Foreground
        {
            get => _foreground;
            set { if (_foreground != value) { _foreground = value; OnPropertyChanged(); } }
        }

        private string _fontFamily = "Segoe UI";
        public string FontFamily
        {
            get => _fontFamily;
            set { if (_fontFamily != value) { _fontFamily = value; OnPropertyChanged(); } }
        }

        private double _fontSize = 12;
        public double FontSize
        {
            get => _fontSize;
            set { if (Math.Abs(_fontSize - value) > double.Epsilon) { _fontSize = value; OnPropertyChanged(); } }
        }

        // ---------- Ukotvení ----------
        private AnchorSides _anchor = AnchorSides.Left | AnchorSides.Top;
        public AnchorSides Anchor
        {
            get => _anchor;
            set { if (_anchor != value) { _anchor = value; OnPropertyChanged(); } }
        }

        private DockTo _dock = DockTo.None;
        public DockTo Dock
        {
            get => _dock;
            set { if (_dock != value) { _dock = value; OnPropertyChanged(); } }
        }

        // ---------- Klonování (využívá VM/SelectionService) ----------
        public virtual FieldComponentBase Clone()
            => (FieldComponentBase)MemberwiseClone();
    }
}
