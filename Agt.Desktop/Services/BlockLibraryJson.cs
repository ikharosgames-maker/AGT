using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Jednoduchá JSON knihovna: každý blok = 1 soubor .json.
    /// Umí číst Key/Version/Title i z vnořených struktur (Block.*, Definition.*, Metadata.*, ...).
    /// Při selhání použije fallback z názvu souboru: KEY__VERSION.json
    /// </summary>
    public sealed class BlockLibraryJson : IBlockLibrary
    {
        public string LibraryRoot { get; }

        public static BlockLibraryJson Default { get; } = new BlockLibraryJson();

        public BlockLibraryJson(string? customRoot = null)
        {
            if (!string.IsNullOrWhiteSpace(customRoot))
            {
                LibraryRoot = customRoot!;
            }
            else
            {
                // %AppData%\AGT\Blocks (Win) | ~/.config/AGT/Blocks (Linux) | ~/Library/Application Support/AGT/Blocks (macOS)
                string baseDir;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT");
                else
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "AGT");

                LibraryRoot = Path.Combine(baseDir, "Blocks");
            }

            Directory.CreateDirectory(LibraryRoot);
        }

        public void SaveToLibrary(object blockDto)
        {
            if (blockDto == null) return;

            // Serializuj a rovnou si z JSONu vytáhni Key/Version/Name (tolerantně)
            var json = JsonSerializer.Serialize(blockDto, new JsonSerializerOptions { WriteIndented = true });

            string key = "unknown";
            string version = "1.0.0";
            string name = "unknown";

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                key = TryReadByPaths(root, KeyPaths()) ?? key;
                version = TryReadByPaths(root, VersionPaths()) ?? version;
                name = TryReadByPaths(root, NamePaths()) ?? key;
            }
            catch
            {
                // necháme defaulty
            }

            var fileName = $"{San(key)}__{San(version)}.json";
            var path = Path.Combine(LibraryRoot, fileName);
            File.WriteAllText(path, json);
        }

        public IEnumerable<BlockCatalogItem> Enumerate()
        {
            foreach (var file in Directory.EnumerateFiles(LibraryRoot, "*.json", SearchOption.TopDirectoryOnly))
            {
                BlockCatalogItem? item = null;
                try
                {
                    var json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);

                    var (key, version, name) = ExtractHeader(doc, file);

                    item = new BlockCatalogItem
                    {
                        Key = key ?? "unknown",
                        Version = version ?? "1.0.0",
                        Name = name ?? (key ?? "unknown"),
                        FilePath = file
                    };
                }
                catch
                {
                    // fallback čistě z filename
                    TryParseFromFileName(file, out var fk, out var fv);
                    item = new BlockCatalogItem
                    {
                        Key = fk ?? "unknown",
                        Version = fv ?? "1.0.0",
                        Name = fk ?? "unknown",
                        FilePath = file
                    };
                }

                if (item != null) yield return item;
            }
        }

        public string LoadRawJson(string filePath) => File.ReadAllText(filePath);

        public bool TryLoadByKeyVersion(string key, string version, out JsonDocument? document, out string? filePath)
        {
            document = null;
            filePath = null;

            var keySan = San(key ?? "");
            var verSan = San(version ?? "");

            // 1) přímý název souboru
            var guess = Path.Combine(LibraryRoot, $"{keySan}__{verSan}.json");
            if (File.Exists(guess))
            {
                try
                {
                    document = JsonDocument.Parse(File.ReadAllText(guess));
                    filePath = guess;
                    return true;
                }
                catch { /* fallthrough */ }
            }

            // 2) porovnej podle názvu souboru (sanitizované)
            foreach (var file in Directory.EnumerateFiles(LibraryRoot, "*.json"))
            {
                if (TryParseFromFileName(file, out var fk, out var fv))
                {
                    if (string.Equals(San(fk ?? ""), keySan, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(San(fv ?? ""), verSan, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            document = JsonDocument.Parse(File.ReadAllText(file));
                            filePath = file;
                            return true;
                        }
                        catch { /* fallthrough */ }
                    }
                }
            }

            // 3) fallback – načítat a porovnat podle obsahu JSONu
            foreach (var file in Directory.EnumerateFiles(LibraryRoot, "*.json"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    var (k, v, _) = ExtractHeader(doc, file);

                    if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrWhiteSpace(v))
                    {
                        if (string.Equals(San(k), keySan, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(San(v), verSan, StringComparison.OrdinalIgnoreCase))
                        {
                            document = JsonDocument.Parse(File.ReadAllText(file));
                            filePath = file;
                            return true;
                        }
                    }
                }
                catch { /* ignore */ }
            }

            return false;
        }

        // ===================== Helpers =====================

        private static (string? key, string? version, string? name) ExtractHeader(JsonDocument doc, string file)
        {
            var root = doc.RootElement;

            string? key = TryReadByPaths(root, KeyPaths());
            string? version = TryReadByPaths(root, VersionPaths());
            string? name = TryReadByPaths(root, NamePaths());

            // fallback z názvu souboru
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(version))
            {
                if (TryParseFromFileName(file, out var fk, out var fv))
                {
                    key ??= fk;
                    version ??= fv;
                }
            }

            if (string.IsNullOrWhiteSpace(name)) name = key;
            return (key, version, name);
        }

        private static bool TryParseFromFileName(string file, out string? key, out string? version)
        {
            key = null; version = null;
            try
            {
                var fn = Path.GetFileNameWithoutExtension(file);
                var idx = fn.LastIndexOf("__", StringComparison.Ordinal);
                if (idx <= 0) return false;

                key = fn.Substring(0, idx);
                version = fn.Substring(idx + 2);
                return true;
            }
            catch { return false; }
        }

        private static string San(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Trim();
        }

        private static IEnumerable<string[]> KeyPaths() => new[]
        {
            new[] { "Key" },
            new[] { "Block", "Key" },
            new[] { "Definition", "Key" },
            new[] { "Metadata", "Key" },
            new[] { "Meta", "Key" },
            new[] { "Header", "Key" },
            new[] { "Info", "Key" },
        };

        private static IEnumerable<string[]> VersionPaths() => new[]
        {
            new[] { "Version" },
            new[] { "Block", "Version" },
            new[] { "Definition", "Version" },
            new[] { "Metadata", "Version" },
            new[] { "Meta", "Version" },
            new[] { "Header", "Version" },
            new[] { "Info", "Version" },
            new[] { "Ver" },
        };

        private static IEnumerable<string[]> NamePaths() => new[]
        {
            new[] { "Name" },
            new[] { "Title" },
            new[] { "Block", "Name" },
            new[] { "Block", "Title" },
            new[] { "Definition", "Name" },
            new[] { "Definition", "Title" },
            new[] { "Metadata", "Name" },
            new[] { "Metadata", "Title" },
        };

        private static string? TryReadByPaths(JsonElement root, IEnumerable<string[]> paths)
        {
            foreach (var path in paths)
            {
                if (TryGetString(root, path, out var s)) return s;
            }

            // úplně poslední šance: rekurzivně najít první výskyt podle jména property (1. z požadovaných „koncových“)
            var lookFor = paths.Select(p => p.Last()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var name in lookFor)
            {
                if (TryFindRecursive(root, name, out var s)) return s;
            }

            return null;
        }

        private static bool TryGetString(JsonElement root, string[] path, out string? value)
        {
            value = null;
            var cur = root;

            foreach (var segment in path)
            {
                if (!TryProp(cur, segment, out var v)) return false;
                cur = v;
            }

            value = cur.ValueKind == JsonValueKind.String ? cur.GetString() : cur.ToString();
            return true;
        }

        private static bool TryFindRecursive(JsonElement el, string name, out string? value)
        {
            value = null;

            // projdi vlastní properties
            if (el.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.NameEquals(name) ||
                        string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
                        return true;
                    }

                    // go deeper
                    if (TryFindRecursive(prop.Value, name, out value)) return true;
                }
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in el.EnumerateArray())
                {
                    if (TryFindRecursive(it, name, out value)) return true;
                }
            }

            return false;
        }

        private static bool TryProp(JsonElement el, string propName, out JsonElement val)
        {
            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(propName) ||
                    string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase))
                {
                    val = p.Value; return true;
                }
            }
            val = default;
            return false;
        }
    }
}
