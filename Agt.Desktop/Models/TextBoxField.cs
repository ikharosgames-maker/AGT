namespace Agt.Desktop.Models
{
    public class TextBoxField : FieldComponentBase
    {
        public TextBoxField() { TypeKey = "textbox"; Height = 28; Width = 260; }
        public override FieldComponentBase Clone()
        {
            var n = new TextBoxField
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
