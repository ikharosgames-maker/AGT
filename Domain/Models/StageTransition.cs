// Domain/Models/StageTransition.cs
using System;

namespace Agt.Domain.Models
{
    public sealed class StageTransition
    {
        public Guid Id { get; set; }
        public Guid FromStageId { get; set; }
        public Guid ToStageId { get; set; }

        /// <summary>
        /// Podmínka, kdy je přechod aktivní (PlainJsonCondition / PredicateExpr).
        /// Zatím jsme ji v runtime nevyhodnocovali – to přijde v dalším kole.
        /// </summary>
        public string? ConditionJson { get; set; }

        /// <summary>
        /// Pravidlo pro přiřazení na cílové stage, pokud přechod vede na konkrétní
        /// jiného příjemce než default stage AssignmentRule.
        /// </summary>
        public AssignmentRule? AssignmentRule { get; set; }

        public int Order { get; set; }
    }
}
