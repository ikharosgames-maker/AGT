using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Agt.Desktop.Models
{
    [Flags]
    public enum AnchorSides { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 }
    public enum DockTo { None, Left, Top, Right, Bottom, Fill }

    public abstract class FieldComponentBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {

        // Capabilities – určují, které vlastnosti/editor se mají v UI zobrazovat

        /// <summary>
        /// Má smysl editovat hodnotu (Value / DefaultValue) jako text.
        /// U LabelField např. false.
        /// </summary>
        public virtual bool CanEditValue => true;

        /// <summary>
        /// Má smysl editovat placeholder (watermark).
        /// </summary>
        public virtual bool CanEditPlaceholder => true;

        /// <summary>
        /// Má smysl editovat DefaultValue (string).
        /// U checkboxu false – má vlastní Checked.
        /// </summary>
        public virtual bool CanEditDefaultValue => true;

        /// <summary>
        /// Má smysl editovat Required.
        /// </summary>
        public virtual bool CanEditRequired => true;

        /// <summary>
        /// Má smysl editovat zarovnání textu (TextAlignment).
        /// U LabelField / CheckBoxField typicky false.
        /// </summary>
        public virtual bool CanEditTextAlignment => true;

        /// <summary>
        /// Má smysl editovat Label (popisek).
        /// </summary>
        public virtual bool CanEditLabel => true;

        /// <summary>
        /// Má smysl editovat zarovnání labelu.
        /// </summary>
        public virtual bool CanEditLabelAlignment => true;

        /// <summary>
        /// Má komponenta boolean „checked“ stav, který dává smysl editovat?
        /// </summary>
        public virtual bool HasCheckedState => false;


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ---------- Hodnota ----------
        private object? _value;
        public object? Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _totalWidth;
        public double TotalWidth
        {
            get => _totalWidth;
            set { if (_totalWidth != value) { _totalWidth = value; OnPropertyChanged(); } }
        }

        private double _totalHeight;
        public double TotalHeight
        {
            get => _totalHeight;
            set { if (_totalHeight != value) { _totalHeight = value; OnPropertyChanged(); } }
        }

        protected FieldComponentBase()
        {
            // Výchozí celková velikost = vlastní velikost, dokud si ji někdo nepřepíše
            TotalWidth = Width;
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
        // Label – barvy
        private Brush _labelForeground;
        public Brush LabelForeground
        {
            get => _labelForeground ?? Foreground;
            set
            {
                if (!ReferenceEquals(_labelForeground, value))
                {
                    _labelForeground = value;
                    OnPropertyChanged();
                }
            }
        }

        private Brush _labelBackground = Brushes.Transparent;
        public Brush LabelBackground
        {
            get => _labelBackground;
            set
            {
                if (!ReferenceEquals(_labelBackground, value))
                {
                    _labelBackground = value;
                    OnPropertyChanged();
                }
            }
        }

        // Label – styl písma
        private bool _labelBold;
        public bool LabelBold
        {
            get => _labelBold;
            set
            {
                if (_labelBold != value)
                {
                    _labelBold = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelItalic;
        public bool LabelItalic
        {
            get => _labelItalic;
            set
            {
                if (_labelItalic != value)
                {
                    _labelItalic = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelUnderline;
        public bool LabelUnderline
        {
            get => _labelUnderline;
            set
            {
                if (_labelUnderline != value)
                {
                    _labelUnderline = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _labelStrikeThrough;
        public bool LabelStrikeThrough
        {
            get => _labelStrikeThrough;
            set
            {
                if (_labelStrikeThrough != value)
                {
                    _labelStrikeThrough = value;
                    OnPropertyChanged();
                }
            }
        }

        // Obsah – styl písma (TextBox, TextArea, atd.)
        private bool _fontBold;
        public bool FontBold
        {
            get => _fontBold;
            set
            {
                if (_fontBold != value)
                {
                    _fontBold = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _fontItalic;
        public bool FontItalic
        {
            get => _fontItalic;
            set
            {
                if (_fontItalic != value)
                {
                    _fontItalic = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _fontUnderline;
        public bool FontUnderline
        {
            get => _fontUnderline;
            set
            {
                if (_fontUnderline != value)
                {
                    _fontUnderline = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _fontStrikeThrough;
        public bool FontStrikeThrough
        {
            get => _fontStrikeThrough;
            set
            {
                if (_fontStrikeThrough != value)
                {
                    _fontStrikeThrough = value;
                    OnPropertyChanged();
                }
            }
        }
        // Zarovnání labelu a textu
        private HorizontalAlignment _labelHorizontalAlignment = HorizontalAlignment.Left;
        /// <summary>
        /// Zarovnání labelu uvnitř jeho prostoru (Left/Center/Right).
        /// </summary>
        public HorizontalAlignment LabelHorizontalAlignment
        {
            get => _labelHorizontalAlignment;
            set
            {
                if (_labelHorizontalAlignment != value)
                {
                    _labelHorizontalAlignment = value;
                    OnPropertyChanged();
                }
            }
        }

        private TextAlignment _textAlignment = TextAlignment.Left;
        /// <summary>
        /// Zarovnání textu uvnitř vstupního pole (Left/Center/Right).
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => _textAlignment;
            set
            {
                if (_textAlignment != value)
                {
                    _textAlignment = value;
                    OnPropertyChanged();
                }
            }
        }
        private Brush _background = (Brush)App.Current.FindResource("AppInputBackgroundBrush"); // výchozí: transparent (uživatel může kdykoli nastavit barvu)
        public Brush Background
        {
            get => _background;
            set
            {
                if (!ReferenceEquals(_background, value))
                {
                    _background = value;
                    OnPropertyChanged();
                }
            }
        }

        private Brush _foreground = Brushes.Black; // bezpečný default pro text
        public Brush Foreground
        {
            get => _foreground;
            set
            {
                if (!ReferenceEquals(_foreground, value))
                {
                    _foreground = value;
                    OnPropertyChanged();
                }
            }
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

        // ---------- Klonování ----------
        public virtual FieldComponentBase Clone()
        {
            var copy = (FieldComponentBase)MemberwiseClone();

            // důležité: Brushes jsou Freezable (reference). Potřebujeme vlastní instance.
            if (Background is SolidColorBrush b1) copy.Background = b1.CloneCurrentValue();
            else if (Background is LinearGradientBrush lb1) copy.Background = lb1.CloneCurrentValue();
            else if (Background != null) copy.Background = Background.CloneCurrentValue();

            if (Foreground is SolidColorBrush b2) copy.Foreground = b2.CloneCurrentValue();
            else if (Foreground is LinearGradientBrush lb2) copy.Foreground = lb2.CloneCurrentValue();
            else if (Foreground != null) copy.Foreground = Foreground.CloneCurrentValue();

            return copy;
        }

        // ---------- Data pro výběry (ComboBox apod.) ----------
        public ObservableCollection<OptionItem> Options { get; } = new();

        // ---------- Uživatelský databinding (JSON pro DbAgent) ----------
        public string? DataBinding { get; set; }

        // ========= INotifyDataErrorInfo =========
        private readonly Dictionary<string, List<string>> _errors =
            new(StringComparer.OrdinalIgnoreCase);

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return _errors.SelectMany(kv => kv.Value).ToList();

            return _errors.TryGetValue(propertyName, out var list)
                ? (IEnumerable)list
                : Array.Empty<string>();
        }

        /// <summary>
        /// Veřejné kvůli DbAgentu (volá reflexí).
        /// </summary>
        public void SetErrors(string propertyName, IEnumerable<string>? messages)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (messages == null || !messages.Any())
            {
                if (_errors.Remove(propertyName))
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                return;
            }

            _errors[propertyName] = messages.ToList();
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Zkopíruje vizuální vlastnosti z jiné komponenty – důležité: klonuje Brushes,
        /// aby se nepřenášely reference (sdílené štětce).
        /// </summary>
        public void CopyVisualsFrom(FieldComponentBase other)
        {
            if (other.Background is SolidColorBrush b1) Background = b1.CloneCurrentValue();
            else if (other.Background is LinearGradientBrush lb1) Background = lb1.CloneCurrentValue();
            else if (other.Background != null) Background = other.Background.CloneCurrentValue();

            if (other.Foreground is SolidColorBrush b2) Foreground = b2.CloneCurrentValue();
            else if (other.Foreground is LinearGradientBrush lb2) Foreground = lb2.CloneCurrentValue();
            else if (other.Foreground != null) Foreground = other.Foreground.CloneCurrentValue();

            FontFamily = other.FontFamily;
            FontSize = other.FontSize;
        }
    }
}
