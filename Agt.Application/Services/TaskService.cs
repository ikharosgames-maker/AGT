// Agt.Application/Services/TaskService.cs
using System.Text.Json;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ICaseRepository _cases;
    private readonly ITaskRepository _tasks;
    private readonly INotificationService _notifications;

    public TaskService(
        ICaseRepository cases,
        ITaskRepository tasks,
        INotificationService notifications)
    {
        _cases = cases;
        _tasks = tasks;
        _notifications = notifications;
    }

    /// <summary>
    /// Nastaví přiřazení pro daný CaseBlock a k němu svázaný TaskItem.
    /// </summary>
    public void Assign(Guid caseBlockId, Guid? userId, Guid? groupId, DateTime? dueAt, Guid actor)
    {
        var cb = _cases.GetBlock(caseBlockId);
        if (cb is null)
            throw new InvalidOperationException($"CaseBlock {caseBlockId} nebyl nalezen.");

        // aktualizace CaseBlocku (stav úkolu na bloku)
        cb.AssigneeUserId = userId;
        cb.AssigneeGroupId = groupId;
        cb.DueAt = dueAt;
        if (cb.State == CaseBlockState.Done || cb.State == CaseBlockState.Rejected)
            cb.State = CaseBlockState.Open;

        _cases.UpsertBlock(cb);

        // TaskItem – jeden na CaseBlock
        var t = _tasks.GetByCaseBlock(caseBlockId);
        if (t is null)
        {
            t = new TaskItem
            {
                Id = Guid.NewGuid(),
                CaseBlockId = cb.Id,
                Status = Domain.Models.TaskStatus.Open,
                AssigneeUserId = userId,
                AssigneeGroupId = groupId,
                DueAt = dueAt,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            t.AssigneeUserId = userId;
            t.AssigneeGroupId = groupId;
            t.DueAt = dueAt;
            if (t.Status == Domain.Models.TaskStatus.Done || t.Status == Domain.Models.TaskStatus.Rejected)
                t.Status = Domain.Models.TaskStatus.Open;
        }

        _tasks.Upsert(t);

        // notifikace o novém/změněném přiřazení
        _notifications.EmitAssigned(cb.Id);
    }

    /// <summary>
    /// Změní stav TaskItemu a zrcadlově i CaseBlock.State.
    /// </summary>
    public void SetStatus(Guid caseBlockId, string status, Guid actor)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("status is required", nameof(status));

        var cb = _cases.GetBlock(caseBlockId);
        if (cb is null)
            throw new InvalidOperationException($"CaseBlock {caseBlockId} nebyl nalezen.");

        var t = _tasks.GetByCaseBlock(caseBlockId);
        if (t is null)
        {
            t = new TaskItem
            {
                Id = Guid.NewGuid(),
                CaseBlockId = cb.Id,
                CreatedAt = DateTime.UtcNow
            };
        }

        // mapování string -> TaskStatus
        Domain.Models.TaskStatus taskStatus = status.Trim().ToLowerInvariant() switch
        {
            "open" => Domain.Models.TaskStatus.Open,
            "inprogress" or "in-progress" => Domain.Models.TaskStatus.InProgress,
            "waiting" => Domain.Models.TaskStatus.Waiting,
            "done" => Domain.Models.TaskStatus.Done,
            "rejected" => Domain.Models.TaskStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Neznámý status úkolu.")
        };

        t.Status = taskStatus;
        _tasks.Upsert(t);

        // promítnout do CaseBlocku
        cb.State = taskStatus switch
        {
            Domain.Models.TaskStatus.Open or Domain.Models.TaskStatus.InProgress => CaseBlockState.Open,
            Domain.Models.TaskStatus.Waiting => CaseBlockState.Waiting,
            Domain.Models.TaskStatus.Done => CaseBlockState.Done,
            Domain.Models.TaskStatus.Rejected => CaseBlockState.Rejected,
            _ => cb.State
        };
        _cases.UpsertBlock(cb);

        _notifications.EmitStatusChanged(cb.Id);
    }
}
