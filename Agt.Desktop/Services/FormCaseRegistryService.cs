using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Agt.Domain.Models; // kvůli FormStatus enumu

namespace Agt.Desktop.Services
{
    public sealed class FormCaseRegistryService : IFormCaseRegistryService
    {
        public FormPublishPaths RegisterPublished(string formKey, string version, JsonNode editedFormJson)
        {
            var formsDir = GetDir("forms");
            var versDir = GetDir("form-versions");
            var layouts = GetDir("layouts");
            Directory.CreateDirectory(formsDir);
            Directory.CreateDirectory(versDir);
            Directory.CreateDirectory(layouts);

            // 1) Form – najdi podle Key, jinak vytvoř nový se stabilním Id
            var (formId, formPath) = GetOrCreateForm(formsDir, formKey);

            // 2) Nová verze
            var formVersionId = Guid.NewGuid();

            // 3) Ulož form.json – nastav CurrentVersionId
            var formObj = new JsonObject
            {
                ["Id"] = formId.ToString(),
                ["Key"] = formKey,
                ["Name"] = formKey,
                ["CreatedAt"] = DateTime.UtcNow,
                ["CreatedBy"] = Guid.Empty.ToString(),
                ["CurrentVersionId"] = formVersionId.ToString()
            };
            WritePretty(formPath, formObj);

            // 4) Sestav a ulož layout ({FormVersionId}.json)
            var layout = BuildLayoutFromEditorJson(formId, formKey, version, editedFormJson);
            var layoutPath = Path.Combine(layouts, formVersionId.ToString("D") + ".json");
            WritePretty(layoutPath, layout);

            // 5) BlockPins (unikátní {BlockId, Version} z layoutu)
            var pins = new JsonArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (layout["Blocks"] is JsonArray blks)
            {
                foreach (var n in blks.OfType<JsonObject>())
                {
                    var id = n["BlockId"]?.ToString();
                    var ver = n["Version"]?.ToString();
                    if (Guid.TryParse(id, out _) && !string.IsNullOrWhiteSpace(ver))
                    {
                        if (seen.Add(id + "|" + ver))
                            pins.Add(new JsonObject { ["BlockId"] = id, ["Version"] = ver });
                    }
                }
            }

            // 6) Ulož form-version ({FormVersionId}.json) – Status jako číslo enumu!
            var fvObj = new JsonObject
            {
                ["Id"] = formVersionId.ToString(),
                ["FormId"] = formId.ToString(),
                ["Version"] = version,
                ["Status"] = (int)FormStatus.Published,
                ["CreatedAt"] = DateTime.UtcNow,
                ["CreatedBy"] = Guid.Empty.ToString(),
                ["BlockPinsJson"] = pins.ToJsonString()
            };
            var fvPath = Path.Combine(versDir, $"{formVersionId}.json");
            WritePretty(fvPath, fvObj);

            // 7) Ověření – když něco chybí, vyhoď chybu, ať to hned vidíš
            if (!File.Exists(formPath)) throw new IOException($"Nevznikl forms soubor: {formPath}");
            if (!File.Exists(fvPath)) throw new IOException($"Nevznikl form-version soubor: {fvPath}");
            if (!File.Exists(layoutPath)) throw new IOException($"Nevznikl layout soubor: {layoutPath}");

            return new FormPublishPaths(formPath, fvPath, layoutPath);
        }

        // ===== Helpers =======================================================

        private static (Guid formId, string formPath) GetOrCreateForm(string formsDir, string formKey)
        {
            foreach (var file in Directory.EnumerateFiles(formsDir, "*.json"))
            {
                try
                {
                    var jo = JsonNode.Parse(File.ReadAllText(file)) as JsonObject;
                    var key = jo?["Key"]?.ToString();
                    var idStr = jo?["Id"]?.ToString();
                    if (string.Equals(key, formKey, StringComparison.OrdinalIgnoreCase) &&
                        Guid.TryParse(idStr, out var existingId))
                        return (existingId, file);
                }
                catch { /* ignore */ }
            }
            var newId = Guid.NewGuid();
            var newPath = Path.Combine(formsDir, $"{newId}.json");
            return (newId, newPath);
        }

        private static JsonObject BuildLayoutFromEditorJson(Guid formId, string formKey, string version, JsonNode edited)
        {
            var stagesOut = new JsonArray();
            var routesOut = new JsonArray();
            var blocksOut = new JsonArray();

            if (edited?["Stages"] is JsonArray stages)
            {
                foreach (var s in stages.OfType<JsonObject>())
                {
                    var sid = Guid.TryParse(s["Id"]?.ToString(), out var g) ? g : Guid.NewGuid();
                    stagesOut.Add(new JsonObject
                    {
                        ["Id"] = sid.ToString(),
                        ["Title"] = s["Title"]?.ToString() ?? "Stage",
                        ["X"] = TryDouble(s, "X") ?? 100,
                        ["Y"] = TryDouble(s, "Y") ?? 100,
                        ["Width"] = TryDouble(s, "Width") ?? 600,
                        ["Height"] = TryDouble(s, "Height") ?? 400
                    });

                    if (s["Blocks"] is JsonArray blocks)
                    {
                        foreach (var b in blocks.OfType<JsonObject>())
                        {
                            var bidStr = b["BlockId"]?.ToString();
                            if (!Guid.TryParse(bidStr, out var bid)) continue; // striktně
                            var ver = b["Version"]?.ToString() ?? "1.0.0";
                            var title = b["Title"]?.ToString() ?? "";

                            blocksOut.Add(new JsonObject
                            {
                                ["BlockId"] = bid.ToString(),
                                ["Version"] = ver,
                                ["Title"] = title,
                                ["StageId"] = sid.ToString(),
                                ["X"] = TryDouble(b, "X") ?? 12,
                                ["Y"] = TryDouble(b, "Y") ?? 12,
                                ["Width"] = TryDouble(b, "Width") ?? 260,
                                ["Height"] = TryDouble(b, "Height") ?? 140
                            });
                        }
                    }
                }
            }

            var routesNode = edited?["Routes"] ?? edited?["StageRoutes"];
            if (routesNode is JsonArray rr)
            {
                foreach (var r in rr.OfType<JsonObject>())
                {
                    routesOut.Add(new JsonObject
                    {
                        ["FromStageId"] = r["FromStageId"]?.ToString() ?? "",
                        ["ToStageId"] = r["ToStageId"]?.ToString() ?? "",
                        // DŮLEŽITÉ: nekopírovat přímo r["Condition"], ale dát klon
                        ["Condition"] = r["Condition"] is JsonNode c ? c.DeepClone() : JsonValue.Create("")
                    });
                }
            }

            return new JsonObject
            {
                ["Form"] = new JsonObject
                {
                    ["Id"] = formId.ToString(),
                    ["Key"] = formKey,
                    ["Name"] = formKey,
                    ["Version"] = version
                },
                ["Stages"] = stagesOut,
                ["StageRoutes"] = routesOut,
                ["Blocks"] = blocksOut
            };
        }

        private static double? TryDouble(JsonObject o, string name)
        {
            var v = o[name];
            if (v is JsonValue val)
            {
                if (val.TryGetValue<double>(out var d)) return d;
                if (double.TryParse(val.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d2)) return d2;
            }
            return null;
        }

        private static string GetDir(string name)
        {
            try
            {
                var t = Type.GetType("Agt.Infrastructure.JsonStore.JsonPaths, Agt.Infrastructure");
                var mi = t?.GetMethod("Dir", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var r = mi?.Invoke(null, new object?[] { name }) as string;
                if (!string.IsNullOrWhiteSpace(r)) return r!;
            }
            catch { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", name);
        }

        private static void WritePretty(string path, JsonObject obj)
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(path)) File.Replace(tmp, path, null, true); else File.Move(tmp, path);
        }
    }
}
