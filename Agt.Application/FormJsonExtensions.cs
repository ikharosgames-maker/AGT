using System;
using System.Text.Json.Nodes;

namespace Agt.Application
{
    public static class FormJsonExtensions
    {
        public static Guid GetId(this JsonNode? node)
        {
            if (node == null) return Guid.Empty;
            var n = node["Id"] ?? node["FormVersionId"];
            return n != null && Guid.TryParse(n.ToString(), out var g) ? g : Guid.Empty;
        }

        public static Guid GetFormId(this JsonNode? node)
        {
            if (node == null) return Guid.Empty;
            var n = node["FormId"];
            return n != null && Guid.TryParse(n.ToString(), out var g) ? g : Guid.Empty;
        }

        public static string? GetName(this JsonNode? node)
        {
            if (node == null) return null;
            return node["Name"]?.ToString()
                ?? node["Title"]?.ToString()
                ?? node["Key"]?.ToString();
        }

        public static string? GetVersionString(this JsonNode? node)
        {
            if (node == null) return null;
            return node["Version"]?.ToString();
        }

        public static JsonNode? GetStageGraphJson(this JsonNode? node)
        {
            if (node == null) return null;
            return node["StageGraphJson"]
                ?? node["StageGraph"]
                ?? node["Graph"];
        }
    }
}
