namespace Agt.Desktop.Models
{
    public class TextAreaField : FieldComponentBase
    {
        public TextAreaField() { TypeKey = "textarea"; Height = 100; }

        private int _rows = 4;
        public int Rows { get => _rows; set { if (_rows != value) { _rows = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new TextAreaField
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
                Rows = Rows
            };
    }
}
