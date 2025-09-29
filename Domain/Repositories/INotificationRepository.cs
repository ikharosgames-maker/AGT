// Agt.Domain/Repositories/INotificationRepository.cs
using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface INotificationRepository
{
    void Add(Notification n);
    IEnumerable<Notification> ListRecent(int take = 50);
}
