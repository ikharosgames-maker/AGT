namespace Agt.Desktop.Models
{
    public class CheckBoxField : FieldComponentBase
    {
        public CheckBoxField() { TypeKey = "checkbox"; Height = 28; }

        private bool _isCheckedDefault;
        public bool IsCheckedDefault { get => _isCheckedDefault; set { if (_isCheckedDefault != value) { _isCheckedDefault = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new CheckBoxField
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
                IsCheckedDefault = IsCheckedDefault
            };
    }
}
