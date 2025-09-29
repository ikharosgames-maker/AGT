namespace Agt.Domain.Primitives;

public enum BoolOperator { And, Or }
public record Condition(BoolOperator Operator, IReadOnlyList<PredicateExpr> Conditions);
public record PredicateExpr(string Field, string Op, object? Value);
