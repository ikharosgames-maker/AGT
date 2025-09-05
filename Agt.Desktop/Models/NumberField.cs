namespace Agt.Desktop.Models
{
    public class NumberField : FieldComponentBase
    {
        public NumberField() { TypeKey = "number"; Height = 28; Width = 120; }
        public override FieldComponentBase Clone() => new NumberField
        {
            FieldKey = FieldKey,
            Label = Label,
            Required = Required,
            Placeholder = Placeholder,
            DefaultValue = DefaultValue,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            ZIndex = ZIndex
        };
    }
}
