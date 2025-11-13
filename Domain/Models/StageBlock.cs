// Agt.Domain/Models/StageBlock.cs
namespace Agt.Domain.Models;

public sealed class StageBlock
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stage this block belongs to.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// Pinned definition of the block.
    /// </summary>
    public Guid BlockDefinitionId { get; set; }

    /// <summary>
    /// Order of the block within the stage.
    /// </summary>
    public int Order { get; set; }
}
