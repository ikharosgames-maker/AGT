// Agt.Domain/Repositories/ITaskRepository.cs
using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface ITaskRepository
{
    TaskItem? GetByCaseBlock(Guid caseBlockId);
    void Upsert(TaskItem task);
}
