// Domain/Models/AssignmentRule.cs
using System;

namespace Agt.Domain.Models
{
    /// <summary>
    /// Jednoduché pravidlo pro přiřazení úkolu/stage.
    /// Lze rozšířit (např. o výrazy nebo user-settings), ale zatím držíme strukturovaný základ.
    /// </summary>
    public sealed class AssignmentRule
    {
        /// <summary>Cílový uživatel. Pokud je null, může se použít Group nebo fallback.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Cílová skupina. Pokud je null, může se použít User nebo fallback.</summary>
        public Guid? GroupId { get; set; }

        /// <summary>
        /// Volitelný výraz / JSON pro dynamické vyhodnocení (např. podle user settings).
        /// Prozatím se nevyhodnocuje, ale je připravený pro rozšíření.
        /// </summary>
        public string? Expression { get; set; }

        /// <summary>
        /// SLA v hodinách od okamžiku založení úkolu. Pokud je null, termín není nastaven.
        /// </summary>
        public int? DueInHours { get; set; }
    }
}
