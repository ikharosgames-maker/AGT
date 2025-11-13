// Agt.Domain/Repositories/IStageRepository.cs
using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

/// <summary>
/// Definition-time view on stages and their blocks for a given form version.
/// </summary>
public interface IStageRepository
{
    StageDefinition? Get(Guid id);

    IEnumerable<StageDefinition> ListByFormVersion(Guid formVersionId);

    IEnumerable<StageBlock> ListBlocks(Guid stageId);

    /// <summary>
    /// Atomically upserts the stage definition and its blocks.
    /// Existing blocks for the stage that are not present in <paramref name="blocks"/>
    /// may be removed by the implementation.
    /// </summary>
    void Upsert(StageDefinition stage, IEnumerable<StageBlock> blocks);
}
