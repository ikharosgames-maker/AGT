using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Agt.Domain.Models
{
    public sealed class FormCaseDto
    {
        public Guid Id { get; set; }

        public Guid FormVersionId { get; set; }

        public string? BusinessKey { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }

        public List<StageStateDto> Stages { get; set; } = new();
    }

    public sealed class StageStateDto
    {
        public Guid StageDefinitionId { get; set; }
        public Guid StageInstanceId { get; set; }

        public string Status { get; set; } = "Open";

        public DateTime? StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public List<BlockStateDto> Blocks { get; set; } = new();
    }

    public sealed class BlockStateDto
    {
        public Guid BlockInstanceId { get; set; }

        public string Status { get; set; } = "Open";

        public JsonNode? Data { get; set; }
    }
}
