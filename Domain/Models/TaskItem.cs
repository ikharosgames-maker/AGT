namespace Agt.Domain.Models;

public enum TaskStatus { Open, InProgress, Waiting, Done, Rejected }

public sealed class TaskItem
{
    public Guid Id { get; set; }
    public Guid CaseBlockId { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Open;
    public Guid? AssigneeUserId { get; set; }
    public Guid? AssigneeGroupId { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
