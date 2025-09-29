using Agt.Domain.Primitives;

namespace Agt.Domain.Models;

public sealed class Route
{
    public Guid Id { get; set; }
    public Guid FormVersionId { get; set; }
    public string FromBlockKey { get; set; } = "";
    public string ToBlockKey { get; set; } = "";
    public Condition Condition { get; set; } = new(BoolOperator.And, Array.Empty<PredicateExpr>());
}
