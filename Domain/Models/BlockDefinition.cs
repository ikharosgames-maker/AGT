// Agt.Domain/Models/BlockDefinition.cs
namespace Agt.Domain.Models;

public enum BlockState { Active, Deprecated }

public sealed class BlockDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";      // stabilní identifikátor (např. "QC_Input")
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.0.0"; // SemVer
    public BlockState State { get; set; } = BlockState.Active;
    public string SchemaJson { get; set; } = "{}";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
