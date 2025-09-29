// JsonNotificationRepository.cs
using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonNotificationRepository : BaseJsonRepo, INotificationRepository
{
    public JsonNotificationRepository() : base("notifications") { }

    public void Add(Notification n)
    {
        if (n.Id == Guid.Empty) n.Id = Guid.NewGuid();
        Save(n.Id, n).GetAwaiter().GetResult();
    }

    public IEnumerable<Notification> ListRecent(int take = 50)
        => AllFiles()
           .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
           .Take(take)
           .Select(f => JsonSerializer.Deserialize<Notification>(File.ReadAllText(f))!)
           .Where(x => x is not null);
}
