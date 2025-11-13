// Agt.Domain/Models/StageDefinition.cs
namespace Agt.Domain.Models;

public sealed class StageDefinition
{
    /// <summary>
    /// Technical identifier of the stage definition.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Form version this stage belongs to.
    /// </summary>
    public Guid FormVersionId { get; set; }

    /// <summary>
    /// Stable business key of the stage (e.g. "QC", "Validation").
    /// </summary>
    public string StageKey { get; set; } = string.Empty;

    /// <summary>
    /// Display title for UI.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Order of the stage in the workflow definition.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Free-form metadata serialized as JSON (routing hints, UI options, etc.).
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Přiřazovací pravidlo pro úkoly v této stage – pokud je nastavené,
    /// může se použít jako defaultní přiřazení (user/group).
    /// </summary>
    public AssignmentRule? AssignmentRule { get; set; }
}
