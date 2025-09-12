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

        private static SolidColorBrush DefaultTextBrush =>
            (Application.Current?.Resources["ControlTextBrush"] as SolidColorBrush)
            ?? Brushes.Black;

        public FieldComponentBase Create(string key, double x, double y, object? defaults)
        {
            FieldComponentBase f = key switch
            {
                "label" => new LabelField { Width = 180, Height = 24, Label = "Label" },
                "textbox" => new TextBoxField { Width = 300, Height = 52, Label = "Text", Placeholder = "zadejte text…" },
                "textarea" => new TextAreaField { Width = 420, Height = 120, Label = "Víceřádkový text" },
                "combobox" => new ComboBoxField { Width = 300, Height = 32, Label = "Výběr" },
                "checkbox" => new CheckBoxField { Width = 200, Height = 28, Label = "Zaškrtnout", IsCheckedDefault = false },
                "date" => new DateField { Width = 260, Height = 32, Label = "Datum" },
                "number" => new NumberField { Width = 260, Height = 32, Label = "Číslo" },
                _ => new LabelField { Width = 180, Height = 24, Label = "Label" }
            };

            f.TypeKey = key;
            f.X = x; f.Y = y;
            f.Background = DefaultFieldBrush;
            f.Foreground = DefaultTextBrush;

            // Pojmenování (typ_blok_label_index) – zatím bez indexu/bloku => doplníme v navazující iteraci z VM
            f.Name = $"{key}_item";
            f.FieldKey = $"{key}_item";

            return f;
        }
    }
}
