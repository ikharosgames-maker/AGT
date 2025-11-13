namespace Agt.Desktop.Models
{
    public class CheckBoxField : FieldComponentBase
    {
        public bool IsCheckedDefault { get; set; }
        public CheckBoxField() { TypeKey = "checkbox"; Height = 24; Width = 200; }
        public override FieldComponentBase Clone()
        {
            var n = new CheckBoxField
            {
                FieldKey = FieldKey,
                Label = Label,
                Required = Required,
                Placeholder = Placeholder,
                DefaultValue = DefaultValue,
                IsCheckedDefault = IsCheckedDefault,
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
