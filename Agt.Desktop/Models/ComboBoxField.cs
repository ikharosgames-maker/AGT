using System.Collections.ObjectModel;

namespace Agt.Desktop.Models
{
    public class ComboBoxField : FieldComponentBase
    {
        public ObservableCollection<string> Options { get; } = new();
        public bool IsEditable { get; set; } = false;

        public ComboBoxField() { TypeKey = "combobox"; Height = 28; Width = 260; }

        public override FieldComponentBase Clone()
        {
            var n = new ComboBoxField
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
            // POZOR: Options je jen getter (read-only kolekce) a je typu ObservableCollection<string>
            foreach (var s in Options)
                n.Options.Add(s);

            n.CopyVisualsFrom(this);
            return n;
        }


    }
}
