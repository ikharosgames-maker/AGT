using System.Text.Json;
using System.Text.Json.Nodes;
using Agt.Domain.Primitives;

namespace Agt.Application.Routing;

public static class PlainJsonConditionEvaluator
{
    public static bool Evaluate(Condition cond, JsonObject data)
    {
        bool EvalPred(PredicateExpr p)
        {
            // lookup pole "Section.Field" v JSON objektu
            JsonNode? node = data;
            foreach (var part in p.Field.Split('.'))
            {
                node = (node as JsonObject)?[part];
                if (node is null) break;
            }

            switch (p.Op)
            {
                case "is-null": return node is null || node.GetValue<object?>() is null;
                case "not-null": return !(node is null || node.GetValue<object?>() is null);
            }

            object? left = node?.GetValue<object?>();
            object? right = p.Value;

            int CmpAsDouble(object? a, object? b)
            {
                double da = Convert.ToDouble(a ?? 0);
                double db = Convert.ToDouble(b ?? 0);
                return da.CompareTo(db);
            }

            bool Eq(object? a, object? b)
                => JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);

            return p.Op switch
            {
                "==" => Eq(left, right),
                "!=" => !Eq(left, right),
                ">" => CmpAsDouble(left, right) > 0,
                ">=" => CmpAsDouble(left, right) >= 0,
                "<" => CmpAsDouble(left, right) < 0,
                "<=" => CmpAsDouble(left, right) <= 0,
                "in" => right is JsonArray arr && arr.Any(x => Eq(x, left)),
                "not-in" => right is JsonArray arr2 && !arr2.Any(x => Eq(x, left)),
                _ => false
            };
        }

        var results = cond.Conditions.Select(EvalPred);
        return cond.Operator == BoolOperator.And ? results.All(x => x) : results.Any(x => x);
    }
}
