// Agt.Application/Services/CaseService.cs
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

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
        IFormRepository forms,
        ICaseRepository cases,
        ITaskRepository tasks,
        IRouteRepository routes,
        INotificationService notif)          // ← názvy rozhraní uveď přesně, jak je máš v projektu
    {
        _forms = forms ?? throw new ArgumentNullException(nameof(forms));
        _cases = cases ?? throw new ArgumentNullException(nameof(cases));
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _routes = routes ?? throw new ArgumentNullException(nameof(routes));
        _notif = notif ?? throw new ArgumentNullException(nameof(notif));
    }

    public Guid StartCase(Guid formVersionId, Guid actor, StartSelection selection)
    {
        // 1) Načti verzi
        var fv = _forms.GetVersion(formVersionId)
                 ?? throw new InvalidOperationException($"FormVersion '{formVersionId:D}' not found.");

        // 2) Namapuj piny: BlockId (GUID) -> Version (string)
        var pinVersionById = new Dictionary<Guid, string>();
        if (!string.IsNullOrWhiteSpace(fv.BlockPinsJson))
        {
            try
            {
                if (System.Text.Json.Nodes.JsonNode.Parse(fv.BlockPinsJson) is System.Text.Json.Nodes.JsonArray arr)
                {
                    foreach (var jo in arr.OfType<System.Text.Json.Nodes.JsonObject>())
                    {
                        var idStr = jo["BlockId"]?.ToString();
                        var ver = jo["Version"]?.ToString() ?? "";
                        if (Guid.TryParse(idStr, out var bid))
                            pinVersionById[bid] = ver;
                    }
                }
            }
            catch
            {
                // když piny nepřečteme, validace níže selže a ukáže přesnou chybu
            }
        }

        // 3) Uživatelský výběr z dialogu (GUIDy jako stringy) -> Guid
        //    POZOR: pokud má StartSelection jinou property než "Blocks", přepiš ji zde.
        var requested = (selection?.Blocks ?? Array.Empty<string>())
            .Select(Guid.Parse) // vyhodí jasnou chybu, když by byl vstup nevalidní
            .ToList();

        // 4) Validace – všechny vybrané musí být v BlockPinsJson
        var notPinned = requested.Where(id => !pinVersionById.ContainsKey(id)).ToList();
        if (notPinned.Count > 0)
            throw new InvalidOperationException($"Block '{notPinned[0]:D}' is not pinned in FormVersion.");

        // 5) Založ Case
        var c = new Case
        {
            Id = Guid.NewGuid(),
            FormVersionId = fv.Id,
            StartedBy = actor,
            StartedAt = DateTime.UtcNow
            // žádný CaseStatus nepoužíváme (u tebe třída Case tu property nemá)
        };
        _cases.Upsert(c);

        // 6) Otevři počáteční bloky = založ CaseBlock + Task(Open) pro každý vybraný GUID
        foreach (var bid in requested)
        {
            var version = pinVersionById[bid]; // verze z pinů
            OpenBlock(c.Id, bid.ToString("D"), version);
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

        // zavři task
        var t = _tasks.GetByCaseBlock(cb.Id);
        if (t is not null)
        {
            t.Status = Domain.Models.TaskStatus.Done;
            _tasks.Upsert(t);
            _notif.EmitStatusChanged(cb.Id);
        }

        // --- připrav si pins pro danou FormVersion (jednou, ne v každé iteraci) ---
        var formVersionId = GetFormVersionIdForCase(cb.CaseId);
        var fv = _forms.GetVersion(formVersionId)
                 ?? throw new InvalidOperationException($"FormVersion '{formVersionId:D}' not found.");

        var pinVersionById = new Dictionary<Guid, string>();
        if (!string.IsNullOrWhiteSpace(fv.BlockPinsJson))
        {
            try
            {
                if (JsonNode.Parse(fv.BlockPinsJson) is JsonArray arr)
                {
                    foreach (var jo in arr.OfType<JsonObject>())
                    {
                        var idStr = jo["BlockId"]?.ToString();
                        var ver = jo["Version"]?.ToString() ?? "";
                        if (Guid.TryParse(idStr, out var bid))
                            pinVersionById[bid] = ver;
                    }
                }
            }
            catch
            {
                // když se piny nepodaří přečíst, necháme mapu prázdnou
            }
        }

        // najdi všechny routy z tohoto bloku
        var routes = _routes.List(formVersionId)
                            .Where(r => string.Equals(r.FromBlockKey, cb.BlockKey, StringComparison.OrdinalIgnoreCase))
                            .ToList();

        // data bloku jako JsonObject
        var data = JsonNode.Parse(cb.DataJson) as JsonObject ?? new JsonObject();

        foreach (var r in routes)
        {
            if (!Routing.PlainJsonConditionEvaluator.Evaluate(r.Condition, data))
                continue;

            // ToBlockKey očekáváme jako GUID string
            if (!Guid.TryParse(r.ToBlockKey, out var toBlockId))
                continue; // nebo throw, pokud je to pro tebe fatální

            // dohledat publikovanou verzi cílového bloku ve FormVersion pins
            if (!pinVersionById.TryGetValue(toBlockId, out var targetVersion))
                continue; // není "pinned" → přeskoč nebo vyhoď vyjímku dle preferencí

            // otevři cílový blok s nalezenou verzí
            OpenBlock(cb.CaseId, r.ToBlockKey, targetVersion);
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

    private void OpenBlock(Guid caseId, string blockKey, string version)
    {
        var cb = new CaseBlock
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            BlockKey = blockKey,                   // GUID string
            BlockDefinitionId = Guid.Parse(blockKey),
            BlockVersion = version ?? "",
            Title = "",                            // volitelně doplnit z knihovny bloků
            DataJson = new JsonObject { ["BlockVersion"] = version ?? "" }.ToJsonString(),
            State = CaseBlockState.Open
        };
        _cases.UpsertBlock(cb);

        var t = _tasks.GetByCaseBlock(cb.Id);
        if (t is null)
        {
            t = new TaskItem
            {
                Id = Guid.NewGuid(),
                CaseBlockId = cb.Id,
                Status = Domain.Models.TaskStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            t.Status = Domain.Models.TaskStatus.Open;
        }
        _tasks.Upsert(t);

        _notif.EmitStatusChanged(cb.Id);
    }
}
