namespace Agt.Domain.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}
