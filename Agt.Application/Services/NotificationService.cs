// Agt.Application/Services/NotificationService.cs
using Agt.Domain.Abstractions;

namespace Agt.Application.Services;

public sealed class NotificationService : INotificationService
{
    public void EmitAssigned(Guid caseBlockId) { /* TODO */ }
    public void EmitStatusChanged(Guid caseBlockId) { /* TODO */ }
    public void EmitDueSoon(Guid caseBlockId) { /* TODO */ }
    public void EmitOverdue(Guid caseBlockId) { /* TODO */ }
}
