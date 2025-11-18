namespace Agt.Desktop.Models
{
    public class NumberField : FieldComponentBase
    {
        public NumberField()
        {
            TypeKey = "number";
            Height = 28;
            Width = 120;

            // čísla defaultně zarovnat doprava
            TextAlignment = System.Windows.TextAlignment.Right;
        }

        public override FieldComponentBase Clone()
        {
            var n = new NumberField
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
                // Váš NumberField NEMÁ Min/Max/Step -> odstraněno
            };

            n.CopyVisualsFrom(this);
            return n;
        }
    }
}
