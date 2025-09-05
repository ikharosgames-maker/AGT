using System.Collections.ObjectModel;

namespace Agt.Desktop.Services
{
    public class FieldCatalogService
    {
        public record FieldDescriptor(string Key, string DisplayName, object? Defaults);

        public ObservableCollection<FieldDescriptor> Items { get; } = new();

        public FieldCatalogService()
        {
            // Základní „číselník“ komponent (lze později načítat z DB/JSON)
            Items.Add(new FieldDescriptor("label", "Popisek (Label)", new { FontSize = 14 }));
            Items.Add(new FieldDescriptor("textbox", "Textové pole (TextBox)", new { Placeholder = "Zadejte text…" }));
            Items.Add(new FieldDescriptor("textarea", "Textová oblast", new { Placeholder = "Více řádků…" }));
            Items.Add(new FieldDescriptor("combobox", "Rozbalovací seznam", new { IsEditable = false, Options = new[] { "A", "B", "C" } }));
            Items.Add(new FieldDescriptor("checkbox", "Zaškrtávátko", new { IsCheckedDefault = false }));
            Items.Add(new FieldDescriptor("date", "Datum", null));
            Items.Add(new FieldDescriptor("number", "Číslo", null));
        }
    }
}
