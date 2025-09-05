namespace Agt.Desktop.Models
{
    public class NumberField : FieldComponentBase
    {
        public NumberField() { TypeKey = "number"; }

        private double _min = double.NaN; public double Min { get => _min; set { if (_min != value) { _min = value; OnPropertyChanged(); } } }
        private double _max = double.NaN; public double Max { get => _max; set { if (_max != value) { _max = value; OnPropertyChanged(); } } }
        private int _decimals = 0; public int Decimals { get => _decimals; set { if (_decimals != value) { _decimals = value; OnPropertyChanged(); } } }

        public override FieldComponentBase Clone()
            => new NumberField
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
                Min = Min,
                Max = Max,
                Decimals = Decimals
            };
    }
}
