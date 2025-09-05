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
            var c = new ComboBoxField
            {
                FieldKey = FieldKey,
                Label = Label,
                Required = Required,
                Placeholder = Placeholder,
                DefaultValue = DefaultValue,
                IsEditable = IsEditable,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                ZIndex = ZIndex
            };
            foreach (var o in Options) c.Options.Add(o);
            return c;
        }
    }
}
