namespace Agt.Domain.Abstractions;
using Agt.Domain.Primitives;

public interface IRoutingService
{
    void AddRoute(Guid formVersionId, string fromBlockKey, string toBlockKey, Condition condition);
    IReadOnlyList<RouteDto> List(Guid formVersionId);
    IReadOnlyList<string> Validate(Guid formVersionId);
    IReadOnlyList<string> EvaluateSatisfiedTargets(Guid caseId, Guid currentCaseBlockId);
}
public record RouteDto(Guid Id, string FromBlockKey, string ToBlockKey, Condition Condition);