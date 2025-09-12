using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public partial class ColorPickerWindow : Window, INotifyPropertyChanged
    {
        private byte _a, _r, _g, _b;
        private string _hex = "#FFFFFFFF";

        public event PropertyChangedEventHandler? PropertyChanged;

        public Brush PreviewBrush => new SolidColorBrush(Color.FromArgb(_a, _r, _g, _b));

        public byte A { get => _a; set { _a = value; OnChanged(); } }
        public byte R { get => _r; set { _r = value; OnChanged(); } }
        public byte G { get => _g; set { _g = value; OnChanged(); } }
        public byte B { get => _b; set { _b = value; OnChanged(); } }

        public string Hex
        {
            get => _hex;
            set { _hex = value; OnChanged(); }
        }

        public SolidColorBrush Result { get; private set; } = new SolidColorBrush(Colors.White);

        public ColorPickerWindow(SolidColorBrush? initial)
        {
            InitializeComponent();
            if (initial != null)
            {
                A = initial.Color.A; R = initial.Color.R; G = initial.Color.G; B = initial.Color.B;
                Hex = $"#{A:X2}{R:X2}{G:X2}{B:X2}";
            }
            DataContext = this;
        }

        private void OnChanged()
        {
            Hex = $"#{A:X2}{R:X2}{G:X2}{B:X2}";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(A)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(R)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(G)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(B)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hex)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewBrush)));
        }

        private static bool TryParseHex(string input, out Color color)
        {
            color = Colors.White;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var s = input.Trim();
            if (!s.StartsWith("#")) s = "#" + s;

            if (s.Length == 7) // #RRGGBB -> doplníme A
                s = "#FF" + s.Substring(1);

            if (s.Length != 9) return false;

            try
            {
                byte a = byte.Parse(s.Substring(1, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(s.Substring(3, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(s.Substring(5, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(s.Substring(7, 2), NumberStyles.HexNumber);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
            catch { return false; }
        }

        private void ApplyHex_Click(object sender, RoutedEventArgs e)
        {
            if (TryParseHex(Hex, out var c))
            {
                _a = c.A; _r = c.R; _g = c.G; _b = c.B;
                OnChanged();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = new SolidColorBrush(Color.FromArgb(A, R, G, B));
            DialogResult = true;
        }
    }
}
