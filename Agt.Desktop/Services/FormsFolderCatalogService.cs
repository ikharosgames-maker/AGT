using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Agt.Desktop.Services
{
    public interface IFormCatalogService
    {
        IEnumerable<FormCatalogItem> Enumerate();
        JsonNode? LoadJson(string filePath, out string formKey);
    }

    public sealed class FormsFolderCatalogService : IFormCatalogService
    {
        private readonly string _root;
        public FormsFolderCatalogService(string? customRoot = null)
        {
            _root = customRoot ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AGT", "forms");
            Directory.CreateDirectory(_root);
        }

        public IEnumerable<FormCatalogItem> Enumerate()
        {
            foreach (var file in Directory.EnumerateFiles(_root, "*.json", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parts = name.Split(new[] { "__" }, StringSplitOptions.None);
                var key = parts.ElementAtOrDefault(0) ?? "unknown";
                var ver = parts.ElementAtOrDefault(1) ?? "";
                yield return new FormCatalogItem
                {
                    Key = key,
                    Version = ver,
                    Title = name,
                    FilePath = file,
                    ModifiedUtc = File.GetLastWriteTimeUtc(file)
                };
            }
        }

        public JsonNode? LoadJson(string filePath, out string formKey)
        {
            formKey = "Process";
            try
            {
                var text = File.ReadAllText(filePath);
                var node = JsonNode.Parse(text);
                if (node is JsonObject o)
                {
                    formKey = o["Key"]?.ToString() ?? formKey;
                }
                return node;
            }
            catch { return null; }
        }
    }

    public sealed class FormCatalogItem
    {
        public string Key { get; set; } = "";
        public string Version { get; set; } = "";
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime ModifiedUtc { get; set; }
        public string Display => $"{Title}   [{Key}]   {Version}";
    }
}
