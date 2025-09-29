namespace Agt.Domain.Abstractions;

public interface ITaskService
{
    void Assign(Guid caseBlockId, Guid? userId, Guid? groupId, DateTime? dueAt, Guid actor);
    void SetStatus(Guid caseBlockId, string status, Guid actor);
}
