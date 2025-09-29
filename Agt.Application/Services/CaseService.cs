// Agt.Application/Services/CaseService.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Application.Services;

public sealed class CaseService : ICaseService
{
    private readonly IFormRepository _forms;
    private readonly IBlockRepository _blocks;
    private readonly IRouteRepository _routes;
    private readonly ICaseRepository _cases;
    private readonly ITaskRepository _tasks;
    private readonly INotificationService _notif;
    private readonly IAuthZ _authZ;

    public CaseService(
        IFormRepository forms, IBlockRepository blocks, IRouteRepository routes,
        ICaseRepository cases, ITaskRepository tasks, INotificationService notif, IAuthZ authZ)
    {
        _forms = forms; _blocks = blocks; _routes = routes;
        _cases = cases; _tasks = tasks; _notif = notif; _authZ = authZ;
    }

    public Guid StartCase(Guid formVersionId, Guid actor, StartSelection selection)
    {
        var fv = _forms.GetVersion(formVersionId) ?? throw new InvalidOperationException("FormVersion not found.");
        var c = new Case
        {
            Id = Guid.NewGuid(),
            FormVersionId = fv.Id,
            StartedBy = actor,
            StartedAt = DateTime.UtcNow,
            StartSelectionJson = JsonSerializer.Serialize(selection)
        };
        _cases.Upsert(c);

        // nacti pinovane bloky z FV
        var pins = JsonSerializer.Deserialize<List<BlockPin>>(fv.BlockPinsJson) ?? new();
        var startKeys = selection.Blocks;
        foreach (var key in startKeys)
        {
            var pin = pins.FirstOrDefault(p => p.Key == key)
                      ?? throw new InvalidOperationException($"Block '{key}' is not pinned in FormVersion.");
            OpenBlock(c.Id, key, pin.Version);
        }
        return c.Id;
    }

    public void CompleteBlock(Guid caseBlockId, Guid actor)
    {
        var cb = _cases.GetBlock(caseBlockId) ?? throw new InvalidOperationException("CaseBlock not found.");
        // lock & done
        cb.State = CaseBlockState.Locked;
        cb.LockedBy = actor;
        cb.LockedAt = DateTime.UtcNow;
        _cases.UpsertBlock(cb);

        // zavri task
        var t = _tasks.GetByCaseBlock(cb.Id);
        if (t is not null) { t.Status = Domain.Models.TaskStatus.Done; _tasks.Upsert(t); _notif.EmitStatusChanged(cb.Id); }

        // najdi všechny splněné routy z tohoto bloku
        var routes = _routes.List(GetFormVersionIdForCase(cb.CaseId))
                            .Where(r => r.FromBlockKey == cb.BlockKey)
                            .ToList();

        // data bloku jako JsonObject
        JsonObject data = JsonNode.Parse(cb.DataJson) as JsonObject ?? new JsonObject();

        foreach (var r in routes)
        {
            if (Routing.PlainJsonConditionEvaluator.Evaluate(r.Condition, data))
            {
                // dohledat pinned verzi cílového bloku ve FormVersion
                var fv = _forms.GetVersion(GetFormVersionIdForCase(cb.CaseId))!;
                var pins = JsonSerializer.Deserialize<List<BlockPin>>(fv.BlockPinsJson) ?? new();
                var pin = pins.FirstOrDefault(p => p.Key == r.ToBlockKey);
                if (pin is null) continue; // nebo vyhodit, podle preferencí

                OpenBlock(cb.CaseId, r.ToBlockKey, pin.Version);
            }
        }
    }

    public void ReopenBlock(Guid caseBlockId, Guid actor, string reason)
    {
        // právo
        var cb = _cases.GetBlock(caseBlockId) ?? throw new InvalidOperationException("CaseBlock not found.");
        if (!_authZ.CanReopenLockedBlocks(actor, GetFormVersionIdForCase(cb.CaseId)))
            throw new UnauthorizedAccessException("Nemáte právo na reopen.");

        if (cb.State != CaseBlockState.Locked)
            throw new InvalidOperationException("Reopen je možný pouze z Locked.");

        cb.State = CaseBlockState.Open;
        cb.ReopenedBy = actor;
        cb.ReopenedAt = DateTime.UtcNow;
        cb.ReopenReason = reason;
        _cases.UpsertBlock(cb);

        // nový task
        var nt = new TaskItem
        {
            Id = Guid.NewGuid(),
            CaseBlockId = cb.Id,
            Status = Domain.Models.TaskStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        _tasks.Upsert(nt);
        _notif.EmitStatusChanged(cb.Id);
    }

    private Guid GetFormVersionIdForCase(Guid caseId)
        => _cases.Get(caseId)?.FormVersionId ?? throw new InvalidOperationException("Case missing.");

    private void OpenBlock(Guid caseId, string blockKey, string blockVersion)
    {
        var cb = new CaseBlock
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            Title = blockKey,
            BlockKey = blockKey,
            BlockVersion = blockVersion,
            DataJson = "{}",
            State = CaseBlockState.Open
        };
        _cases.UpsertBlock(cb);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            CaseBlockId = cb.Id,
            Status = Domain.Models.TaskStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        _tasks.Upsert(task);
        _notif.EmitAssigned(cb.Id);
    }

    private sealed record BlockPin(string Key, string Version);
}
