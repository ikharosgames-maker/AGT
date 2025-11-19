using System.Windows;

namespace Agt.Desktop.Models
{
    public class LabelField : FieldComponentBase
    {
        public override bool CanEditValue => false;
        public override bool CanEditPlaceholder => false;
        public override bool CanEditDefaultValue => false;
        public override bool CanEditRequired => false;
        public override bool CanEditTextAlignment => false;

        public LabelField()
        {
            TypeKey = "label";
            Width = 120;
            Height = 24;
            Label = "Label";
        }

        public override FieldComponentBase Clone()
        {
            var l = new LabelField
            {
                FieldKey = FieldKey,
                Label = Label,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                ZIndex = ZIndex
            };

            l.CopyVisualsFrom(this);
            return l;
        }
    }
}
