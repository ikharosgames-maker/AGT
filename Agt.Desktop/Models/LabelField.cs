namespace Agt.Desktop.Models
{
    public class LabelField : FieldComponentBase
    {
        public LabelField() { TypeKey = "label"; Height = 28; }

        private double _fontSize = 14;
        public double FontSize { get => _fontSize; set { if (_fontSize != value) { _fontSize = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new LabelField
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
                FontSize = FontSize
            };
    }
}
