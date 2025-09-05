using System.Collections.ObjectModel;

namespace Agt.Desktop.Models
{
    public class ComboBoxField : FieldComponentBase
    {
        public ComboBoxField() { TypeKey = "combobox"; }

        public ObservableCollection<string> Options { get; } = new();

        private bool _isEditable;
        public bool IsEditable { get => _isEditable; set { if (_isEditable != value) { _isEditable = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
        {
            var c = new ComboBoxField
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                ZIndex = ZIndex,
                FieldKey = FieldKey,
                Label = Label,
                Required = Required,
                Placeholder = Placeholder,
                DefaultValue = DefaultValue,
                IsEditable = IsEditable
            };
            foreach (var o in Options) c.Options.Add(o);
            return c;
        }
    }
}
