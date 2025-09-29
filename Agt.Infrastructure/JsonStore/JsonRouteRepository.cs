using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonRouteRepository : BaseJsonRepo, IRouteRepository
{
    public JsonRouteRepository() : base("routes") { }

    public IEnumerable<Route> List(Guid formVersionId)
        => AllFiles()
           .Select(f => JsonSerializer.Deserialize<Route>(File.ReadAllText(f)))
           .Where(r => r is not null && r!.FormVersionId == formVersionId)!
           .Select(r => r!);

    public void Add(Route route)
    {
        if (route.Id == Guid.Empty) route.Id = Guid.NewGuid();
        Save(route.Id, route).GetAwaiter().GetResult();
    }
}
