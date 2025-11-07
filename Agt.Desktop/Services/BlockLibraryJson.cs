using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Agt.Infrastructure.JsonStore;

namespace Agt.Desktop.Services
{
    public sealed class BlockLibEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        public override string ToString() => string.IsNullOrWhiteSpace(Title) ? $"{Key} ({Version})" : Title;

        // dovolí výraz 'if (!entry)' jako test na null
        public static bool operator !(BlockLibEntry? e) => e is null;
    }

    public sealed class BlockLibraryJson : IBlockLibrary
    {
        public static BlockLibraryJson Default { get; } = new BlockLibraryJson();

        public string LibraryRoot { get; }

        public BlockLibraryJson(string? customRoot = null)
        {
            LibraryRoot = string.IsNullOrWhiteSpace(customRoot) ? JsonPaths.Dir("blocks") : customRoot!;
            Directory.CreateDirectory(LibraryRoot);
        }

        public IEnumerable<BlockLibEntry> Enumerate()
        {
            foreach (var path in Directory.EnumerateFiles(LibraryRoot, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var key = name;
                var version = "1.0";

                var parts = name.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) { key = parts[0]; version = parts[1]; }

                var title = TryReadTitle(path) ?? key;

                yield return new BlockLibEntry
                {
                    Key = key,
                    Version = version,
                    Title = title,
                    FilePath = path
                };
            }
        }

        public BlockLibEntry? TryFind(string key, string version)
        {
            return Enumerate().FirstOrDefault(e =>
                string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));
        }

        public string GetPath(BlockLibEntry entry) => entry.FilePath;

        public void Save(BlockLibEntry entry, string json)
        {
            var path = string.IsNullOrWhiteSpace(entry.FilePath)
                ? Path.Combine(LibraryRoot, $"{entry.Key}__{entry.Version}.json")
                : entry.FilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            entry.FilePath = path;
        }

        // ---- Finální API (string) ----
        public BlockLibEntry SaveToLibrary(string blockName, string blockVersion, string json, string? title = null)
        {
            if (string.IsNullOrWhiteSpace(blockName)) throw new ArgumentException("blockName missing");
            if (string.IsNullOrWhiteSpace(blockVersion)) blockVersion = "1.0";
            var entry = TryFind(blockName, blockVersion) ?? new BlockLibEntry { Key = blockName, Version = blockVersion, Title = title ?? blockName };
            Save(entry, json);
            return entry;
        }

        public BlockLibEntry SaveToLibrary(string blockName, string blockVersion, object value, string? title = null, JsonSerializerOptions? options = null)
        {
            var json = JsonSerializer.Serialize(value, options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web){ WriteIndented = true });
            return SaveToLibrary(blockName, blockVersion, json, title);
        }

        // ---- Overload pro volání s Guid + pojmenovanými argumenty 'key' a 'blockName' ----
        public bool SaveToLibrary(Guid blockId, string blockVersion, JsonElement root, string? key = null, string? blockName = null)
        {
            try
            {
                var json = root.GetRawText();
                var k = blockId.ToString("D");
                var title = !string.IsNullOrWhiteSpace(blockName) ? blockName : (key ?? k);
                var entry = TryFind(k, blockVersion) ?? new BlockLibEntry { Key = k, Version = blockVersion, Title = title };
                Save(entry, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? TryReadTitle(string path)
        {
            try
            {
                using var s = File.OpenRead(path);
                using var doc = JsonDocument.Parse(s);
                if (doc.RootElement.TryGetProperty("Title", out var t)) return t.GetString();
                if (doc.RootElement.TryGetProperty("Block", out var b) &&
                    b.TryGetProperty("Title", out var bt)) return bt.GetString();
                if (doc.RootElement.TryGetProperty("Definition", out var d) &&
                    d.TryGetProperty("Title", out var dt)) return dt.GetString();
                if (doc.RootElement.TryGetProperty("Metadata", out var m) &&
                    m.TryGetProperty("Title", out var mt)) return mt.GetString();
            }
            catch { }
            return null;
        }
    }
}
