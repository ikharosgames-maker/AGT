namespace Agt.Desktop.Models
{
    public class LabelField : FieldComponentBase
    {
        public double FontSize { get; set; } = 14;
        public LabelField() { TypeKey = "label"; Height = 24; Width = 160; }
        public override FieldComponentBase Clone() => new LabelField
        {
            FieldKey = FieldKey,
            Label = Label,
            Required = Required,
            Placeholder = Placeholder,
            DefaultValue = DefaultValue,
            FontSize = FontSize,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZIndex = ZIndex
        };
    }
}
