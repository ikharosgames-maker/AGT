using System;

namespace Agt.Domain.Models
{
    /// <summary>
    /// Záznam v indexu formulářů (přehled verzí).
    /// </summary>
    public sealed class FormRepositoryEntry
    {
        /// <summary>
        /// Id pro interní práci – typicky FormVersionId nebo FormId podle implementace repozitáře.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Klíč formuláře (technický identifikátor, např. "customer-intake").
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Uživatelský název formuláře (pro zobrazení v UI).
        /// DOPLNĚNO kvůli kódu typu "f.Name" ve StageRunWindow apod.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Verze (např. "1.0.0").
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Je tato verze draft?
        /// </summary>
        public bool IsDraft { get; set; }

        /// <summary>
        /// Cesta k fyzickému JSON souboru s FormVersion.
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;
    }
}
