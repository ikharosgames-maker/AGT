using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public static class LayoutStore
    {
        private static string Dir(string name)
        {
            try
            {
                var t = Type.GetType("Agt.Infrastructure.JsonStore.JsonPaths, Agt.Infrastructure");
                var mi = t?.GetMethod("Dir", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var r = mi?.Invoke(null, new object?[] { name }) as string;
                if (!string.IsNullOrWhiteSpace(r))
                {
                    Directory.CreateDirectory(r!);
                    return r!;
                }
            }
            catch { }
            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", name);
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        public static string GetLayoutPath(Guid formVersionId)
            => Path.Combine(Dir("layouts"), formVersionId.ToString("D") + ".json");

        public static bool TryOpenLayout(Guid formVersionId, out JsonObject? layout, out string path)
        {
            path = GetLayoutPath(formVersionId);
            layout = null;
            if (!File.Exists(path)) return false;
            try
            {
                var jo = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
                if (jo == null) return false;
                layout = jo;
                return true;
            }
            catch { return false; }
        }
    }
}
