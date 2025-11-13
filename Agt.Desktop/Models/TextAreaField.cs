namespace Agt.Desktop.Models
{
    public class TextAreaField : FieldComponentBase
    {
        public TextAreaField() { TypeKey = "textarea"; Height = 80; Width = 300; }
        public override FieldComponentBase Clone()
        {
            var n = new TextAreaField
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
                // TextAreaField u vás NEMÁ Rows -> nic nepřenášet
            };
            n.CopyVisualsFrom(this);
            return n;
        }


    }
}