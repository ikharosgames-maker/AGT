using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agt.Desktop.Models
{
    /// <summary>
    /// Stav ukotvení – jednoduché booly, ať se dobře binduje v UI.
    /// Default: Left + Top = true.
    /// </summary>
    public class AnchorOptions : INotifyPropertyChanged
    {
        private bool _left = true;   // default
        private bool _top = true;   // default
        private bool _right;
        private bool _bottom;
        private bool _fillX;
        private bool _fillY;
        private bool _fill;

        public bool Left { get => _left; set { if (_left != value) { _left = value; OnPropertyChanged(); } } }
        public bool Top { get => _top; set { if (_top != value) { _top = value; OnPropertyChanged(); } } }
        public bool Right { get => _right; set { if (_right != value) { _right = value; OnPropertyChanged(); } } }
        public bool Bottom { get => _bottom; set { if (_bottom != value) { _bottom = value; OnPropertyChanged(); } } }

        /// <summary>Roztažení vodorovně přes celý parent.</summary>
        public bool FillX { get => _fillX; set { if (_fillX != value) { _fillX = value; OnPropertyChanged(); } } }
        /// <summary>Roztažení svisle přes celý parent.</summary>
        public bool FillY { get => _fillY; set { if (_fillY != value) { _fillY = value; OnPropertyChanged(); } } }
        /// <summary>Roztažení přes celý parent (obě osy).</summary>
        public bool Fill { get => _fill; set { if (_fill != value) { _fill = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
