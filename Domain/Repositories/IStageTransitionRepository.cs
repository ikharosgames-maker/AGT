// Agt.Domain/Repositories/IStageTransitionRepository.cs
using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface IStageTransitionRepository
{
    StageTransition? Get(Guid id);

    IEnumerable<StageTransition> ListByFormVersion(Guid formVersionId);

    IEnumerable<StageTransition> ListOutgoing(Guid fromStageId);

    void Upsert(StageTransition transition);

    void Delete(Guid id);
}
