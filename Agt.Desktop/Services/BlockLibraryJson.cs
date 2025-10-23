using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// JSON knihovna bloků. Canon identita = (BlockId, Version).
    /// Soubory: %AppData%/AGT/blocks/{BlockId}__{Version}.json
    /// </summary>
    public sealed class BlockLibraryJson : IBlockLibrary
    {
        public static IBlockLibrary Default { get; } = new BlockLibraryJson();

        private readonly List<BlockLibEntry> _index = new();
        private readonly List<JsonDocument> _openDocs = new();

        private BlockLibraryJson()
        {
            Reindex();
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

        private static string BlocksDir() => GetDir("blocks");

        private void Reindex()
        {
            _index.Clear();
            Directory.CreateDirectory(BlocksDir());

            foreach (var path in Directory.EnumerateFiles(BlocksDir(), "*.json"))
            {
                try
                {
                    using var jd = JsonDocument.Parse(File.ReadAllText(path));
                    var root = jd.RootElement;

                    if (!root.TryGetProperty("BlockId", out var idEl)) continue;
                    if (!Guid.TryParse(idEl.GetString(), out var bid)) continue;

                    if (!root.TryGetProperty("Version", out var vEl)) continue;
                    var version = vEl.GetString();
                    if (string.IsNullOrWhiteSpace(version)) continue;

                    var key = root.TryGetProperty("Key", out var kEl) ? (kEl.GetString() ?? "") : "";
                    var name = root.TryGetProperty("BlockName", out var nEl) ? (nEl.GetString() ?? key) : key;

                    _index.Add(new BlockLibEntry(bid, key, name, version!, path));
                }
                catch
                {
                    // poškozený soubor ignoruj
                }
            }
        }

        public IEnumerable<BlockLibEntry> Enumerate() => _index;

        public bool TryLoadByIdVersion(Guid blockId, string version,
                                       out JsonDocument? doc, out BlockLibEntry? entry)
        {
            entry = _index.FirstOrDefault(e =>
                e.BlockId == blockId &&
                string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));

            if (entry is null) { doc = null; return false; }

            try
            {
                var jd = JsonDocument.Parse(File.ReadAllText(entry.FilePath));
                _openDocs.Add(jd);
                doc = jd;
                return true;
            }
            catch
            {
                doc = null;
                return false;
            }
        }

        public bool SaveToLibrary(Guid blockId, string version, JsonElement schemaRoot,
                                  string? key = null, string? blockName = null)
        {
            try
            {
                Directory.CreateDirectory(BlocksDir());

                using var buffer = new MemoryStream();
                using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    writer.WriteString("BlockId", blockId.ToString());
                    writer.WriteString("Version", version);

                    if (!string.IsNullOrWhiteSpace(key)) writer.WriteString("Key", key);
                    if (!string.IsNullOrWhiteSpace(blockName)) writer.WriteString("BlockName", blockName);

                    if (schemaRoot.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in schemaRoot.EnumerateObject())
                        {
                            var n = prop.Name;
                            if (n.Equals("BlockId", StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("Version", StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("Key", StringComparison.OrdinalIgnoreCase) ||
                                n.Equals("BlockName", StringComparison.OrdinalIgnoreCase))
                                continue;

                            writer.WritePropertyName(n);
                            prop.Value.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                var filePath = Path.Combine(BlocksDir(), $"{blockId:D}__{version}.json");
                File.WriteAllBytes(filePath, buffer.ToArray());

                // refresh indexu
                _index.RemoveAll(e => e.BlockId == blockId &&
                                      string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));
                using var confirmDoc = JsonDocument.Parse(File.ReadAllText(filePath));
                var root = confirmDoc.RootElement;
                var k = root.TryGetProperty("Key", out var kEl) ? (kEl.GetString() ?? "") : "";
                var nm = root.TryGetProperty("BlockName", out var nEl) ? (nEl.GetString() ?? k) : k;
                _index.Add(new BlockLibEntry(blockId, k, nm, version, filePath));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            foreach (var d in _openDocs) d.Dispose();
            _openDocs.Clear();
        }
    }
}
