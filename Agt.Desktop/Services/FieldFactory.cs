using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Továrna na nové field komponenty.
    /// POZOR: Záměrně nenastavujeme výchozí Background/Foreground,
    /// ponecháváme je NULL => barvy dodá theme + autokontrast v šablonách.
    /// </summary>
    public class FieldFactory
    {
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
            f.X = x;
            f.Y = y;

            // 🔑 Klíčové: žádné tvrdé barvy – necháme NULL,
            // ať zafunguje globální theme + AutoContrastForegroundConverter v šablonách.
            f.Background = null;
            f.Foreground = null;

            // Pokud bys někdy posílal explicitní výchozí vzhledy (např. z dialogu),
            // můžeš je sem propsat – šablony je respektují.
            if (defaults is IFieldVisualDefaults d)
            {
                if (d.Background != null) f.Background = d.Background;
                if (d.Foreground != null) f.Foreground = d.Foreground;
                if (!string.IsNullOrWhiteSpace(d.FontFamily)) f.FontFamily = d.FontFamily;
                if (d.FontSize > 0) f.FontSize = d.FontSize;
            }

            // Pojmenování (můžeš později doplnit index/kontext bloku)
            f.Name = $"{key}_item";
            f.FieldKey = $"{key}_item";

            return f;
        }
    }

    /// <summary>
    /// Volitelné rozhraní pro předání defaultů vzhledu (pokud jej nepoužíváš, klidně smaž).
    /// </summary>
    public interface IFieldVisualDefaults
    {
        Brush? Background { get; }
        Brush? Foreground { get; }
        string? FontFamily { get; }
        double FontSize { get; }
    }
}
