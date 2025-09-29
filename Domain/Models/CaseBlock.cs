namespace Agt.Domain.Models;

public enum CaseBlockState { Open, Waiting, Done, Rejected, Locked }

public sealed class CaseBlock
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid BlockDefinitionId { get; set; }
    public string BlockKey { get; set; } = "";
    public string BlockVersion { get; set; } = ""; // pin z FormVersion.BlockPinsJson

    public string Title { get; set; } = "";
    public string DataJson { get; set; } = "{}";
    public CaseBlockState State { get; set; } = CaseBlockState.Open;

    public Guid? AssigneeUserId { get; set; }
    public Guid? AssigneeGroupId { get; set; }
    public DateTime? DueAt { get; set; }

    public Guid? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? ReopenedBy { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public string? ReopenReason { get; set; }
}
