using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agt.Desktop.Models
{
    public abstract class FieldComponentBase : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TypeKey { get; protected set; } = "";

        private double _x; public double X { get => _x; set { if (_x != value) { _x = value; OnPropertyChanged(); } } }
        private double _y; public double Y { get => _y; set { if (_y != value) { _y = value; OnPropertyChanged(); } } }
        private double _width = 260; public double Width { get => _width; set { if (_width != value) { _width = value; OnPropertyChanged(); } } }
        private double _height = 40; public double Height { get => _height; set { if (_height != value) { _height = value; OnPropertyChanged(); } } }
        private int _zIndex; public int ZIndex { get => _zIndex; set { if (_zIndex != value) { _zIndex = value; OnPropertyChanged(); } } }

        private string _fieldKey = ""; public string FieldKey { get => _fieldKey; set { if (_fieldKey != value) { _fieldKey = value; OnPropertyChanged(); } } }
        private string _label = "Label"; public string Label { get => _label; set { if (_label != value) { _label = value; OnPropertyChanged(); } } }
        private bool _required; public bool Required { get => _required; set { if (_required != value) { _required = value; OnPropertyChanged(); } } }
        private string _placeholder = ""; public string Placeholder { get => _placeholder; set { if (_placeholder != value) { _placeholder = value; OnPropertyChanged(); } } }
        private string _defaultValue = ""; public string DefaultValue { get => _defaultValue; set { if (_defaultValue != value) { _defaultValue = value; OnPropertyChanged(); } } }

        public abstract FieldComponentBase Clone();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
