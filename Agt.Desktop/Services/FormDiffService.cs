using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public enum ChangeKind { Patch, New } // přesně podle tvého zadání

    public interface IFormDiffService
    {
        ChangeKind Classify(JsonNode original, JsonNode edited);
    }

    public sealed class FormDiffService : IFormDiffService
    {
        // Které názvy polí považujeme za "ne-strukturální" (tj. patch)
        private static readonly HashSet<string> PatchOnlyProps = new(StringComparer.OrdinalIgnoreCase)
        {
            "Title","Name","Label","Description","Help","Placeholder",
            "Hint","Tooltip","Default","DefaultValue","Validation","Required",
            "MinLength","MaxLength","Min","Max","Pattern","Format","Mask",
            "Visible","Readonly","Disabled"
        };

        public ChangeKind Classify(JsonNode original, JsonNode edited)
        {
            var o = CollectComponentSignatures(original);
            var n = CollectComponentSignatures(edited);

            // 1) pokud počet/identita komponent nesedí → NEW
            if (o.Count != n.Count) return ChangeKind.New;
            if (!o.SetEquals(n)) return ChangeKind.New;

            // 2) stejná množina komponent → zkontrolujeme, zda se nezměnily strukturální vlastnosti
            // Jednoduché pravidlo: sledujeme, jestli se mimo "PatchOnlyProps" změnilo něco uvnitř komponent.
            // Pokud ano → NEW, jinak PATCH.
            var diffs = FindStructuralDiff(original, edited);
            return diffs ? ChangeKind.New : ChangeKind.Patch;
        }

        private static HashSet<string> CollectComponentSignatures(JsonNode root)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var comp in EnumerateComponents(root))
            {
                var id = ReadString(comp, "Key") ?? ReadString(comp, "Id") ?? ReadString(comp, "FieldKey");
                var typ = ReadString(comp, "Type") ?? ReadString(comp, "FieldType");
                if (!string.IsNullOrWhiteSpace(id))
                    set.Add($"{id}::{typ}");
            }
            return set;
        }

        private static bool FindStructuralDiff(JsonNode a, JsonNode b)
        {
            // Projde objekt/array a pokud najde rozdíl v klíčích, které nejsou "PatchOnlyProps", vrací true.
            if (a is JsonObject oa && b is JsonObject ob)
            {
                var keys = oa.Select(kv => kv.Key).Union(ob.Select(kv => kv.Key), StringComparer.OrdinalIgnoreCase);
                foreach (var k in keys)
                {
                    var av = oa[k];
                    var bv = ob[k];

                    // rozdílná přítomnost klíče
                    if ((av is null) != (bv is null))
                    {
                        if (!PatchOnlyProps.Contains(k)) return true;
                        // Pokud jde o patch-only a jedna strana chybí → stále to bereme jako "patch" (např. doplněný Title)
                        continue;
                    }
                    if (av is null && bv is null) continue;

                    if (av is JsonValue va && bv is JsonValue vb)
                    {
                        // změna hodnoty primitivu – pokud je to patch-only klíč, OK, jinak strukturální diff
                        if (!JsonEquals(va, vb) && !PatchOnlyProps.Contains(k))
                            return true;
                    }
                    else
                    {
                        // rekurze do struktur
                        if (FindStructuralDiff(av!, bv!)) return true;
                    }
                }
                return false;
            }
            if (a is JsonArray aa && b is JsonArray bb)
            {
                // Rozdílné pořadí komponent bereme jako strukturální změnu (většinou layout)
                if (aa.Count != bb.Count) return true;
                for (int i = 0; i < aa.Count; i++)
                    if (FindStructuralDiff(aa[i]!, bb[i]!)) return true;
                return false;
            }
            // jiný druh uzlu → strukturální změna
            return true;
        }

        private static bool JsonEquals(JsonValue a, JsonValue b)
        {
            return a.ToJsonString() == b.ToJsonString();
        }

        private static IEnumerable<JsonObject> EnumerateComponents(JsonNode root)
        {
            var q = new Queue<JsonNode>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur is JsonObject o)
                {
                    // heuristika: typické názvy komponent/field/block
                    if (o.ContainsKey("FieldKey") || o.ContainsKey("Key") || o.ContainsKey("FieldType") || o.ContainsKey("Type"))
                        yield return o;

                    foreach (var kv in o)
                        if (kv.Value is JsonObject or JsonArray) q.Enqueue(kv.Value!);
                }
                else if (cur is JsonArray a)
                {
                    foreach (var el in a)
                        if (el is JsonObject or JsonArray) q.Enqueue(el!);
                }
            }
        }

        private static string? ReadString(JsonObject o, params string[] keys)
        {
            foreach (var k in keys)
                if (o[k] is JsonValue v && v.TryGetValue<string>(out var s))
                    return s;
            return null;
        }
    }
}
