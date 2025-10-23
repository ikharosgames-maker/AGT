using Agt.Domain.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public enum VersionScheme { Counter, AutoTwoLevel }

    public interface IFormSaveService
    {
        // Varianta, když máš originální SOUBOR + aktuální JSON
        string SaveNextVersion(string formsRoot, string originalFilePath, JsonNode edited, out string newVersion);

        // Varianta, když máš originální JSON + aktuální JSON (bez souboru)
        string SaveNextVersionFromJson(string formsRoot, string formKey, JsonNode original, JsonNode edited, out string newVersion);
    }

    public sealed class FormSaveService : IFormSaveService
    {
        private readonly IFormDiffService _diff;
        private readonly IFormVersioningService _ver;

        public FormSaveService(IFormDiffService diff, IFormVersioningService ver)
        {
            _diff = diff;
            _ver = ver;
        }

        public string SaveNextVersion(string formsRoot, string originalFilePath, JsonNode edited, out string newVersion)
        {
            if (!File.Exists(originalFilePath))
                throw new FileNotFoundException("Původní soubor nenalezen.", originalFilePath);

            Directory.CreateDirectory(formsRoot);

            var originalText = File.ReadAllText(originalFilePath);
            var original = JsonNode.Parse(originalText) ?? new JsonObject();

            var formKey = FindFirstString(edited, "Key", "FormKey", "Id", "FormId")
                          ?? FindFirstString(original, "Key", "FormKey", "Id", "FormId")
                          ?? Path.GetFileNameWithoutExtension(originalFilePath).Split("__").FirstOrDefault()
                          ?? "Process";

            return SaveCore(formsRoot, formKey, original, edited, out newVersion);
        }

        public string SaveNextVersionFromJson(string formsRoot, string formKey, JsonNode original, JsonNode edited, out string newVersion)
        {
            Directory.CreateDirectory(formsRoot);
            if (string.IsNullOrWhiteSpace(formKey))
                formKey = FindFirstString(edited, "Key", "FormKey", "Id", "FormId") ?? "Process";

            return SaveCore(formsRoot, formKey, original, edited, out newVersion);
        }

        private string SaveCore(string formsRoot, string formKey, JsonNode original, JsonNode edited, out string newVersion)
        {
            string cur = FindFirstString(original, "Version", "FormVersion", "SchemaVersion") ?? "0.0.0";

            // Auto klasifikace: PATCH, když se nemění počet komponent ve všech blocích; jinak MINOR
            var kind = _diff.Classify(original, edited);
            var bump = (kind == ChangeKind.Patch) ? VersionBump.Patch : VersionBump.Minor;
            newVersion = _ver.ComputeNextFree(formsRoot, formKey, cur, bump);

            EnsureRootHeader(edited, formKey, newVersion);
            SetVersion(edited, newVersion);
            EnsureMetadata(edited, cur);

            string San(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
            var target = Path.Combine(formsRoot, $"{San(formKey)}__{San(newVersion)}.json");

            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(target, edited.ToJsonString(opts));
            return target;
        }

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
                        if (names.Any(n => string.Equals(n, kv.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (kv.Value is JsonValue v && v.TryGetValue<string>(out var s))
                                return s;
                        }
                        if (kv.Value is JsonObject or JsonArray) q.Enqueue(kv.Value!);
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

        private static void SetVersion(JsonNode node, string value)
        {
            // Nastav pouze KOŘENOVOU verzi formuláře.
            if (node is JsonObject o)
            {
                o["Version"] = value;           // form-level verze
                                                // Nepřepisovat žádné vnořené "Version" (bloky, schémata)!
            }
        }

        private static void EnsureRootHeader(JsonNode node, string formKey, string version)
        {
            if (node is not JsonObject o) return;
            if (!o.ContainsKey("Key")) o["Key"] = formKey;
            if (!o.ContainsKey("Name")) o["Name"] = formKey;
            o["Version"] = version;
            o["Status"] = (int)FormStatus.Published;
        }

        private static bool TrySetFirst(JsonNode node, string value, params string[] names)
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
                        if (names.Any(n => string.Equals(n, kv.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            o[kv.Key] = value;
                            return true;
                        }
                        if (kv.Value is JsonObject or JsonArray) q.Enqueue(kv.Value!);
                    }
                }
                else if (cur is JsonArray a)
                {
                    foreach (var el in a)
                        if (el is JsonObject or JsonArray) q.Enqueue(el!);
                }
            }
            return false;
        }

        private static void EnsureMetadata(JsonNode node, string parentVersion)
        {
            if (node is not JsonObject o) return;
            if (o["Metadata"] is not JsonObject meta) { meta = new JsonObject(); o["Metadata"] = meta; }
            meta["ParentVersion"] = parentVersion;
            meta["CreatedUtc"] = DateTime.UtcNow;
        }
    }
}