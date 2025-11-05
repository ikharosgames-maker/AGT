namespace Agt.Desktop.Models
{
    public class LabelField : FieldComponentBase
    {
        public double FontSize { get; set; } = 14;
        public LabelField() { TypeKey = "label"; Height = 24; Width = 160; }
        public override FieldComponentBase Clone()
        {
            var n = new LabelField
            {
                FieldKey = FieldKey,
                Label = Label,
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
