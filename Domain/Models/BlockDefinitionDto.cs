using System;
using System.Collections.Generic;

namespace Agt.Domain.Models
{
    /// <summary>
    /// Sdílený tvar definice bloku – má odpovídat JSONu, který generuje editor bloků.
    /// </summary>
    public sealed class BlockDefinitionDto
    {
        /// <summary>Unikátní identifikátor bloku (GUID).</summary>
        public Guid BlockId { get; set; }

        /// <summary>Logický klíč bloku (např. "CustomerAddress"), může být prázdný.</summary>
        public string? Key { get; set; }

        /// <summary>Zobrazovaný název bloku.</summary>
        public string? BlockName { get; set; }

        /// <summary>Verze bloku (semver-like, např. "1.0.0").</summary>
        public string? Version { get; set; }

        /// <summary>
        /// Verze schématu JSONu – kvůli budoucím migracím.
        /// Pokud chybí, bere se jako "1.0".
        /// </summary>
        public string? SchemaVersion { get; set; }

        /// <summary>Volitelná další metadata (např. popis, tagy) v libovolném formátu.</summary>
        public string? MetadataJson { get; set; }

        /// <summary>Seznam položek (komponent) uvnitř bloku.</summary>
        public List<FieldDefinitionDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Definice jedné položky (komponenty) v bloku – odpovídá JSONu v poli Items.
    /// </summary>
    public sealed class FieldDefinitionDto
    {
        // --- Identita / typ ---

        /// <summary>Typ komponenty (textbox, textarea, checkbox, number, date, combobox...)</summary>
        public string? TypeKey { get; set; }

        /// <summary>Jedinečný klíč pole v rámci bloku.</summary>
        public string? FieldKey { get; set; }

        /// <summary>Zobrazovaný popisek.</summary>
        public string? Label { get; set; }

        /// <summary>Hint/placeholder pro textová pole.</summary>
        public string? Placeholder { get; set; }

        /// <summary>Výchozí hodnota (string, číslo, atd. serializovaná jako text).</summary>
        public string? DefaultValue { get; set; }

        /// <summary>Povinné pole?</summary>
        public bool Required { get; set; }

        // --- Layout / pozice ---

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        /// <summary>Z-index komponenty (pořadí vykreslení).</summary>
        public int ZIndex { get; set; }

        /// <summary>Ukotvení (Left/Right/Top/Bottom) – pokud používáš.</summary>
        public string? Anchor { get; set; }

        /// <summary>Dock (None, Left, Right, Top, Bottom, Fill) – pokud používáš.</summary>
        public string? Dock { get; set; }

        // --- Stylování ---

        public string? Background { get; set; }
        public string? Foreground { get; set; }
        public string? FontFamily { get; set; }
        public double? FontSize { get; set; }
        public string? FontWeight { get; set; }
        public string? FontStyle { get; set; }

        // --- Další volitelná nastavení ---

        /// <summary>Obecná rozšiřitelná metadata komponenty.</summary>
        public string? MetadataJson { get; set; }
    }
}
