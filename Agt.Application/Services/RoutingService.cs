using System.Text.Json.Nodes;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Primitives;
using Agt.Domain.Repositories;
using Agt.Application.Routing;

namespace Agt.Application.Services;

public sealed class RoutingService : IRoutingService
{
    private readonly IRouteRepository _routes;
    private readonly IBlockRepository _blocks;
    private readonly IFormRepository _forms;

    public RoutingService(IRouteRepository routes, IBlockRepository blocks, IFormRepository forms)
    {
        _routes = routes; _blocks = blocks; _forms = forms;
    }

    public void AddRoute(Guid formVersionId, string fromBlockKey, string toBlockKey, Condition condition)
    {
        // jednoduchá validace existence bloků
        var from = _blocks.GetByKey(formVersionId, fromBlockKey);
        var to = _blocks.GetByKey(formVersionId, toBlockKey);
        if (from is null) throw new InvalidOperationException($"From block '{fromBlockKey}' neexistuje.");
        if (to is null) throw new InvalidOperationException($"To block '{toBlockKey}' neexistuje.");

        _routes.Add(new Route
        {
            Id = Guid.NewGuid(),
            FormVersionId = formVersionId,
            FromBlockKey = fromBlockKey,
            ToBlockKey = toBlockKey,
            Condition = condition
        });
    }

    public IReadOnlyList<RouteDto> List(Guid formVersionId)
        => _routes.List(formVersionId)
                  .Select(r => new RouteDto(r.Id, r.FromBlockKey, r.ToBlockKey, r.Condition))
                  .ToList();

    public IReadOnlyList<string> Validate(Guid formVersionId)
    {
        var errors = new List<string>();
        var blocks = _blocks.ListByFormVersion(formVersionId).Select(b => b.Key).ToHashSet();
        foreach (var r in _routes.List(formVersionId))
        {
            if (!blocks.Contains(r.FromBlockKey)) errors.Add($"Route {r.Id}: From '{r.FromBlockKey}' neexistuje.");
            if (!blocks.Contains(r.ToBlockKey)) errors.Add($"Route {r.Id}: To '{r.ToBlockKey}' neexistuje.");
            if (r.Condition.Conditions.Count == 0) errors.Add($"Route {r.Id}: prázdná podmínka.");
        }
        return errors;
    }

    // Vrací seznam ToBlockKey, které jsou splněné pro daná data bloku
    public IReadOnlyList<string> EvaluateSatisfiedTargets(Guid caseId, Guid currentCaseBlockId)
    {
        // v tomto kroku ještě nemáme Case/CaseBlock storage – to doplníme v KROKU 3.
        // Prozatím vrátíme prázdné pole, implementace naváže na CaseRepository.
        return Array.Empty<string>();
    }

    // Pomocná – k vyhodnocení dat (použijeme v KROKU 3)
    internal static bool Match(Route r, JsonObject data) => PlainJsonConditionEvaluator.Evaluate(r.Condition, data);
}
