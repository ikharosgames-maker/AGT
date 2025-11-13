// Agt.Domain/Models/StageTransition.cs
using Agt.Domain.Primitives;

namespace Agt.Domain.Models;

public sealed class StageTransition
{
    public Guid Id { get; set; }

    /// <summary>
    /// Source stage.
    /// </summary>
    public Guid FromStageId { get; set; }

    /// <summary>
    /// Target stage.
    /// </summary>
    public Guid ToStageId { get; set; }

    /// <summary>
    /// Condition under which the transition is taken.
    /// </summary>
    public Condition? Condition { get; set; }

    /// <summary>
    /// Volitelné přiřazovací pravidlo pro úkol v cílové stage.
    /// Pokud je nastavené, může přebít AssignmentRule na StageDefinition.
    /// </summary>
    public AssignmentRule? AssignmentRule { get; set; }
}
