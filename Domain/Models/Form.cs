namespace Agt.Domain.Models;

public sealed class Form
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Key { get; set; } = "";
    public Guid? CurrentVersionId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
