using System;
using System.Collections.Generic;

namespace Agt.Domain.Models
{
    /// <summary>
    /// Sdílené DTO definice bloku – používá ho editor, knihovna bloků i runtime.
    /// </summary>
    public sealed class BlockDefinitionDto
    {
        /// <summary>Globální identifikátor bloku (interní, v UI se typicky nezobrazuje).</summary>
        public Guid BlockId { get; set; }

        /// <summary>Logický klíč bloku (např. CustomerAddress). Volitelný.</summary>
        public string? Key { get; set; }

        /// <summary>Zobrazovaný název bloku.</summary>
        public string BlockName { get; set; } = string.Empty;

        /// <summary>Verze bloku (semver-like, např. "1.0.0").</summary>
        public string? Version { get; set; }

        /// <summary>Verze schématu JSON (kvůli budoucím migracím).</summary>
        public string? SchemaVersion { get; set; }

        /// <summary>Uživatel, který blok vytvořil (aktuálně Windows Environment.UserName).</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Čas založení bloku v UTC.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Velikost mřížky v designeru.</summary>
        public double GridSize { get; set; } = 8;

        /// <summary>Zda se mřížka zobrazuje.</summary>
        public bool ShowGrid { get; set; } = true;

        /// <summary>Zda se komponenty přichytávají k mřížce.</summary>
        public bool SnapToGrid { get; set; } = true;

        /// <summary>Seznam položek (komponent) v bloku.</summary>
        public List<FieldDefinitionDto> Items { get; set; } = new();
    }

    /// <summary>Definice jedné komponenty uvnitř bloku.</summary>
    public sealed class FieldDefinitionDto
    {
        public string TypeKey { get; set; } = string.Empty;
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string FieldKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int ZIndex { get; set; }

        public string? DefaultValue { get; set; }

        public string Background { get; set; } = "#00000000";
        public string Foreground { get; set; } = "#FF000000";

        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 12;
    }
}
