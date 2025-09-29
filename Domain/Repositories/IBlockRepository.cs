using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface IBlockRepository
{
    BlockDefinition? GetByKey(Guid formVersionId, string blockKey);
    IEnumerable<BlockDefinition> ListByFormVersion(Guid formVersionId);
    void Upsert(BlockDefinition def);
}
