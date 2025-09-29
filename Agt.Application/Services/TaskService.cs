// Agt.Application/Services/TaskService.cs
using Agt.Domain.Abstractions;

namespace Agt.Application.Services;

public sealed class TaskService : ITaskService
{
    public void Assign(Guid caseBlockId, Guid? userId, Guid? groupId, DateTime? dueAt, Guid actor)
    { /* TODO */ }

    public void SetStatus(Guid caseBlockId, string status, Guid actor)
    { /* TODO */ }
}
