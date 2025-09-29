namespace Agt.Domain.Models;

public sealed class Case
{
    public Guid Id { get; set; }
    public Guid FormVersionId { get; set; }
    public Guid StartedBy { get; set; }
    public DateTime StartedAt { get; set; }
    public string? StartSelectionJson { get; set; }
}
