namespace Agt.Desktop.Models
{
    public class TextBoxField : FieldComponentBase
    {
        public TextBoxField() { TypeKey = "textbox"; }

        private int _maxLength = 0;
        public int MaxLength { get => _maxLength; set { if (_maxLength != value) { _maxLength = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new TextBoxField
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
                MaxLength = MaxLength
            };
    }
}
