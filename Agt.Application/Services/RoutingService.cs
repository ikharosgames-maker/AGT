// Agt.Application/Services/RoutingService.cs
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
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
    private readonly ICaseRepository _cases;

    public RoutingService(
        IRouteRepository routes,
        IBlockRepository blocks,
        IFormRepository forms,
        ICaseRepository cases)
    {
        _routes = routes;
        _blocks = blocks;
        _forms = forms;
        _cases = cases;
    }

    public void AddRoute(Guid formVersionId, string fromBlockKey, string toBlockKey, Condition condition)
    {
        if (string.IsNullOrWhiteSpace(fromBlockKey))
            throw new ArgumentException("fromBlockKey is required", nameof(fromBlockKey));
        if (string.IsNullOrWhiteSpace(toBlockKey))
            throw new ArgumentException("toBlockKey is required", nameof(toBlockKey));

        var route = new Route
        {
            Id = Guid.NewGuid(),
            FormVersionId = formVersionId,
            FromBlockKey = fromBlockKey,
            ToBlockKey = toBlockKey,
            Condition = condition
        };

        _routes.Add(route);
    }

    public IReadOnlyList<RouteDto> List(Guid formVersionId)
    {
        return _routes
            .List(formVersionId)
            .Select(r => new RouteDto(r.Id, r.FromBlockKey, r.ToBlockKey, r.Condition))
            .ToList();
    }

    /// <summary>
    /// Zkontroluje základní konzistenci definovaných rout – zda cílové bloky
    /// existují ve formě; případné další kontroly lze doplnit.
    /// </summary>
    public IReadOnlyList<string> Validate(Guid formVersionId)
    {
        var errors = new List<string>();

        var form = _forms.GetVersion(formVersionId);
        if (form is null)
        {
            errors.Add($"FormVersion {formVersionId} neexistuje.");
            return errors;
        }

        var routes = _routes.List(formVersionId).ToList();
        if (!routes.Any())
            return errors;

        var blockKeys = _blocks.ListByFormVersion(formVersionId)
                               .Select(b => b.Key)
                               .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var r in routes)
        {
            if (!blockKeys.Contains(r.FromBlockKey))
                errors.Add($"Route {r.Id}: FromBlockKey '{r.FromBlockKey}' neexistuje.");
            if (!blockKeys.Contains(r.ToBlockKey))
                errors.Add($"Route {r.Id}: ToBlockKey '{r.ToBlockKey}' neexistuje.");
        }

        return errors;
    }

    /// <summary>
    /// Najde všechny cílové bloky (ToBlockKey), jejichž podmínka je splněná
    /// pro aktuální data daného CaseBlocku.
    /// </summary>
    public IReadOnlyList<string> EvaluateSatisfiedTargets(Guid caseId, Guid currentCaseBlockId)
    {
        var c = _cases.Get(caseId);
        if (c is null)
            return Array.Empty<string>();

        var cb = _cases.GetBlock(currentCaseBlockId);
        if (cb is null || cb.CaseId != caseId)
            return Array.Empty<string>();

        // data bloku – pokud JSON nejde načíst, bereme prázdný objekt
        JsonObject data;
        try
        {
            var node = JsonNode.Parse(cb.DataJson);
            data = node as JsonObject ?? new JsonObject();
        }
        catch (JsonException)
        {
            data = new JsonObject();
        }

        var routes = _routes
            .List(c.FormVersionId)
            .Where(r => string.Equals(r.FromBlockKey, cb.BlockKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var satisfied = new List<string>();
        foreach (var r in routes)
        {
            if (Match(r, data))
                satisfied.Add(r.ToBlockKey);
        }

        return satisfied;
    }

    // Pomocná – vyhodnocení Condition/PredicateExpr nad JSON daty bloku
    internal static bool Match(Route r, JsonObject data)
        => PlainJsonConditionEvaluator.Evaluate(r.Condition, data);
}
