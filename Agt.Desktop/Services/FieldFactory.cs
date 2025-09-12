using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    public class FieldFactory
    {
        private static SolidColorBrush DefaultFieldBrush =>
            (Application.Current?.Resources["FieldBackgroundBrush"] as SolidColorBrush)
            ?? new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x2E));

        public FieldComponentBase Create(string key, double x, double y, object? defaults)
        {
            FieldComponentBase f = key switch
            {
                "label" => new LabelField { Width = 160, Height = 22 },
                "textbox" => new TextBoxField { Width = 260, Height = 28 },
                "textarea" => new TextAreaField { Width = 360, Height = 90 },
                "combobox" => new ComboBoxField { Width = 260, Height = 28 },
                "checkbox" => new CheckBoxField { Width = 160, Height = 22 },
                "date" => new DateField { Width = 200, Height = 28 },
                "number" => new NumberField { Width = 200, Height = 28 },
                _ => new LabelField { Width = 160, Height = 22 }
            };

            f.TypeKey = key;
            f.X = x; f.Y = y;
            f.Background = DefaultFieldBrush;

            // Pojmenování: typ_blok_label_index (index přidává DesignerViewModel při vkládání, zde necháme základ)
            f.Name = $"{key}_item";
            f.FieldKey = $"{key}_item";

            // defaulty z knihovny (pokud nějaké přijdou)
            // TODO: promítnout podle tvé struktury

            return f;
        }
    }
}
