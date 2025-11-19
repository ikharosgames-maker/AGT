using System.Windows;

namespace Agt.Desktop.Models
{
    public class CheckBoxField : FieldComponentBase
    {
        private bool _isCheckedDefault;

        /// <summary>
        /// Původní vlastnost – výchozí zaškrtnutí (persistována).
        /// </summary>
        public bool IsCheckedDefault
        {
            get => _isCheckedDefault;
            set
            {
                if (_isCheckedDefault != value)
                {
                    _isCheckedDefault = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Checked));
                }
            }
        }

        /// <summary>
        /// Alias na IsCheckedDefault – používá se v UI jako "Checked".
        /// </summary>
        public bool Checked
        {
            get => IsCheckedDefault;
            set => IsCheckedDefault = value;
        }

        // Capabilities – checkbox má svůj Checked, ne textovou DefaultValue, nemá placeholder ani TextAlignment
        public override bool CanEditPlaceholder => false;
        public override bool CanEditDefaultValue => false;
        public override bool CanEditTextAlignment => false;
        public override bool HasCheckedState => true;

        public CheckBoxField()
        {
            TypeKey = "checkbox";
            Width = 120;
            Height = 24;
            Label = "Checkbox";
        }

        public override FieldComponentBase Clone()
        {
            var c = new CheckBoxField
            {
                FieldKey = FieldKey,
                Label = Label,
                Required = Required,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                ZIndex = ZIndex,
                IsCheckedDefault = IsCheckedDefault,
            };

            c.CopyVisualsFrom(this);
            return c;
        }
    }
}
