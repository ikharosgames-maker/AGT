namespace Agt.Desktop.Models
{
    public class DateField : FieldComponentBase
    {
        public DateField() { TypeKey = "date"; Height = 28; Width = 160; }
        public override FieldComponentBase Clone()
        {
            var n = new DateField
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
            n.CopyVisualsFrom(this);
            return n;
        }

    }
}
