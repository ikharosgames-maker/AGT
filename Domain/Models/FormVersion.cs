// Agt.Domain/Models/FormVersion.cs
namespace Agt.Domain.Models;

public sealed class FormVersion
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public string Version { get; set; } = "1.0.0";
    public FormStatus Status { get; set; } = FormStatus.Draft;
    public string? ChangeLog { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }

    // JSON: pole objektů { key, version } – pin na konkrétní verze bloků
    public string BlockPinsJson { get; set; } = "[]";

    // JSON: definice stage grafu (stages, blocks, transitions) pro editor/workflow engine.
    public string StageGraphJson { get; set; } = "{}";
}
