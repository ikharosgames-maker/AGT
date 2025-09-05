using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    public class FieldCatalogService
    {
        public record CatalogItem(string Key, string Category, string DisplayName, string Icon, Dictionary<string, object>? Defaults, Dictionary<string, object>? Meta);

        public IReadOnlyList<CatalogItem> Items { get; private set; } = new List<CatalogItem>();

        private static string CatalogPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "FieldCatalog.json");

        public FieldCatalogService() => LoadOrCreate();

        private void LoadOrCreate()
        {
            try
            {
                if (!File.Exists(CatalogPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
                    File.WriteAllText(CatalogPath, GetDefaultJson());
                }
                var json = File.ReadAllText(CatalogPath);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Items = JsonSerializer.Deserialize<List<CatalogItem>>(json, opts) ?? new List<CatalogItem>();
            }
            catch
            {
                Items = new List<CatalogItem>();
            }
        }

        private static string GetDefaultJson() => @"
[
  { ""Key"": ""label"",    ""Category"": ""Základní"", ""DisplayName"": ""Popisek"",         ""Icon"": ""🏷"", ""Defaults"": { ""Label"": ""Popisek"", ""Width"": 160, ""Height"": 28, ""FieldKey"": ""label1"" } },
  { ""Key"": ""textbox"",  ""Category"": ""Vstup"",    ""DisplayName"": ""Textové pole"",    ""Icon"": ""🔤"", ""Defaults"": { ""Label"": ""Jméno"", ""Placeholder"": ""Zadej jméno"", ""Width"": 280, ""FieldKey"": ""firstName"" } },
  { ""Key"": ""textarea"", ""Category"": ""Vstup"",    ""DisplayName"": ""Víceřádkové"",     ""Icon"": ""📝"", ""Defaults"": { ""Label"": ""Poznámka"", ""Rows"": 4, ""Height"": 100, ""Width"": 380, ""FieldKey"": ""note"" } },
  { ""Key"": ""combobox"", ""Category"": ""Výběr"",    ""DisplayName"": ""Seznam"",          ""Icon"": ""▾"",  ""Defaults"": { ""Label"": ""Stát"", ""Options"": [""CZ"",""SK"",""PL""], ""FieldKey"": ""country"" } },
  { ""Key"": ""checkbox"", ""Category"": ""Výběr"",    ""DisplayName"": ""Zaškrtávátko"",    ""Icon"": ""☑"",  ""Defaults"": { ""Label"": ""Souhlasím"", ""IsCheckedDefault"": false, ""FieldKey"": ""agree"" } },
  { ""Key"": ""date"",     ""Category"": ""Vstup"",    ""DisplayName"": ""Datum"",           ""Icon"": ""📅"", ""Defaults"": { ""Label"": ""Datum narození"", ""Format"": ""yyyy-MM-dd"", ""FieldKey"": ""birthDate"" } },
  { ""Key"": ""number"",   ""Category"": ""Vstup"",    ""DisplayName"": ""Číslo"",           ""Icon"": ""#️⃣"", ""Defaults"": { ""Label"": ""Věk"", ""Min"": 0, ""Max"": 120, ""Decimals"": 0, ""FieldKey"": ""age"" } }
]
";
    }
}
