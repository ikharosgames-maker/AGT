using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonBlockRepository : BaseJsonRepo, IBlockRepository
{
    public JsonBlockRepository() : base("blocks") { }

    public BlockDefinition? GetByKey(Guid formVersionId, string blockKey)
        => ListByFormVersion(formVersionId).FirstOrDefault(b => b.Key == blockKey);

    public IEnumerable<BlockDefinition> ListByFormVersion(Guid formVersionId)
        => AllFiles()
           .Select(f => JsonSerializer.Deserialize<BlockDefinition>(File.ReadAllText(f)))
           .Where(b => b is not null)!      // POZN: pro MVP nefiltrujeme podle formVersionId
           .Select(b => b!);

    public void Upsert(BlockDefinition def)
    {
        if (def.Id == Guid.Empty) def.Id = Guid.NewGuid();
        Save(def.Id, def).GetAwaiter().GetResult();
    }
}
