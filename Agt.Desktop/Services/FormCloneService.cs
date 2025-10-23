using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public interface IFormCloneService
    {
        /// <summary>Načti formulář ze souboru a ulož jako novou verzi.</summary>
        /// <returns>Cesta k novému souboru.</returns>
        string CloneFromFileAsNewVersion(string sourceFile, string formsRoot, VersionBump bump, out string newVersion);
    }

    public sealed class FormCloneService : IFormCloneService
    {
        private readonly IFormVersioningService _ver;
        public FormCloneService(IFormVersioningService ver) => _ver = ver;

        public string CloneFromFileAsNewVersion(string sourceFile, string formsRoot, VersionBump bump, out string newVersion)
        {
            var json = File.ReadAllText(sourceFile);
            var node = JsonNode.Parse(json) ?? new JsonObject();

            string key = FindFirstString(node, "Key", "FormKey", "Id", "FormId") ??
                         Path.GetFileNameWithoutExtension(sourceFile).Split("__").FirstOrDefault() ?? "unknown";

            string curVer = FindFirstString(node, "Version", "FormVersion", "SchemaVersion") ??
                            (Path.GetFileNameWithoutExtension(sourceFile).Split("__").Skip(1).FirstOrDefault() ?? "0.0.0");

            // spočítej cílovou verzi a collision-free variantu
            Directory.CreateDirectory(formsRoot);
            newVersion = _ver.ComputeNextFree(formsRoot, key, curVer, bump);

            // nastav Version na více rozumných místech (nepovinné cesty bezpečně)
            SetFirstString(node, newVersion, "Version", "FormVersion", "SchemaVersion");

            // doplň metadata
            var meta = EnsureObject(node, "Metadata");
            meta["ParentVersion"] = curVer;
            meta["CreatedUtc"] = DateTime.UtcNow;

            // uložení
            string San(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
            var target = Path.Combine(formsRoot, $"{San(key)}__{San(newVersion)}.json");

            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(target, node.ToJsonString(opts));
            return target;
        }

        // --- helpers ---
        private static string? FindFirstString(JsonNode node, params string[] names)
        {
            var q = new System.Collections.Generic.Queue<JsonNode>();
            q.Enqueue(node);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur is JsonObject o)
                {
                    foreach (var kv in o)
                    {
                        if (names.Any(n => string.Equals(kv.Key, n, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (kv.Value is JsonValue v && v.TryGetValue<string>(out var s))
                                return s;
                        }
                        if (kv.Value is JsonObject or JsonArray)
                            q.Enqueue(kv.Value!);
                    }
                }
                else if (cur is JsonArray a)
                {
                    foreach (var el in a)
                        if (el is JsonObject or JsonArray) q.Enqueue(el!);
                }
            }
            return null;
        }

        private static void SetFirstString(JsonNode node, string value, params string[] names)
        {
            var q = new System.Collections.Generic.Queue<JsonNode>();
            q.Enqueue(node);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur is JsonObject o)
                {
                    foreach (var kv in o.ToList())
                    {
                        if (names.Any(n => string.Equals(kv.Key, n, StringComparison.OrdinalIgnoreCase)))
                        {
                            o[kv.Key] = value;
                            return;
                        }
                        if (kv.Value is JsonObject or JsonArray)
                            q.Enqueue(kv.Value!);
                    }
                }
                else if (cur is JsonArray a)
                {
                    foreach (var el in a)
                        if (el is JsonObject or JsonArray) q.Enqueue(el!);
                }
            }
            // pokud nenalezeno, doplň na root
            if (node is JsonObject ro)
                ro["Version"] = value;
        }

        private static JsonObject EnsureObject(JsonNode node, string prop)
        {
            if (node is JsonObject ro)
            {
                if (ro[prop] is JsonObject ok) return ok;
                var created = new JsonObject();
                ro[prop] = created;
                return created;
            }
            return new JsonObject();
        }
    }
}
