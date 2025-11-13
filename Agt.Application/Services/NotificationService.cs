// Agt.Application/Services/NotificationService.cs
using System.Text.Json;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly ICaseRepository _cases;
    private readonly ITaskRepository _tasks;

    public NotificationService(
        INotificationRepository notifications,
        ICaseRepository cases,
        ITaskRepository tasks)
    {
        _notifications = notifications;
        _cases = cases;
        _tasks = tasks;
    }

    public void EmitAssigned(Guid caseBlockId)
    {
        var payload = BuildPayload(caseBlockId, type: "Task.Assigned");
        if (payload is null) return;

        _notifications.Add(payload);
    }

    public void EmitStatusChanged(Guid caseBlockId)
    {
        var payload = BuildPayload(caseBlockId, type: "Task.StatusChanged");
        if (payload is null) return;

        _notifications.Add(payload);
    }

    public void EmitDueSoon(Guid caseBlockId)
    {
        var payload = BuildPayload(caseBlockId, type: "Task.DueSoon");
        if (payload is null) return;

        _notifications.Add(payload);
    }

    public void EmitOverdue(Guid caseBlockId)
    {
        var payload = BuildPayload(caseBlockId, type: "Task.Overdue");
        if (payload is null) return;

        _notifications.Add(payload);
    }

    private Notification? BuildPayload(Guid caseBlockId, string type)
    {
        var cb = _cases.GetBlock(caseBlockId);
        if (cb is null) return null;

        var c = _cases.Get(cb.CaseId);
        var t = _tasks.GetByCaseBlock(caseBlockId);

        var dto = new
        {
            CaseId = cb.CaseId,
            CaseBlockId = cb.Id,
            cb.BlockKey,
            cb.Title,
            CaseFormVersionId = c?.FormVersionId,
            cb.AssigneeUserId,
            cb.AssigneeGroupId,
            cb.DueAt,
            TaskStatus = t?.Status.ToString(),
            Type = type,
            UtcNow = DateTime.UtcNow
        };

        return new Notification
        {
            Id = Guid.NewGuid(),
            Type = type,
            PayloadJson = JsonSerializer.Serialize(dto),
            CreatedAt = DateTime.UtcNow
        };
    }
}
