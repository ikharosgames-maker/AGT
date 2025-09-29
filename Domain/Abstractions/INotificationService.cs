// Agt.Domain/Abstractions/INotificationService.cs
namespace Agt.Domain.Abstractions;

public interface INotificationService
{
    void EmitAssigned(Guid caseBlockId);
    void EmitStatusChanged(Guid caseBlockId);
    void EmitDueSoon(Guid caseBlockId);
    void EmitOverdue(Guid caseBlockId);
}
