namespace Agt.Desktop.Models
{
    public class DateField : FieldComponentBase
    {
        public DateField() { TypeKey = "date"; }

        private string _format = "yyyy-MM-dd";
        public string Format { get => _format; set { if (_format != value) { _format = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new DateField
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
                Format = Format
            };
    }
}
