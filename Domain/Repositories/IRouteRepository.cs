using Agt.Domain.Models;

namespace Agt.Domain.Repositories;

public interface IRouteRepository
{
    IEnumerable<Route> List(Guid formVersionId);
    void Add(Route route);
}
