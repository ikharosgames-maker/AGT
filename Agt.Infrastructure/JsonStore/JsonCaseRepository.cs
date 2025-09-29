using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonCaseRepository : BaseJsonRepo, ICaseRepository
{
    public JsonCaseRepository() : base("cases") { }

    string BlockDir => JsonPaths.Dir("caseblocks");

    public Case? Get(Guid id) => Load<Case>(id).GetAwaiter().GetResult();

    public void Upsert(Case entity)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        Save(entity.Id, entity).GetAwaiter().GetResult();
    }

    public CaseBlock? GetBlock(Guid id)
    {
        var p = Path.Combine(BlockDir, id + ".json");
        return File.Exists(p) ? JsonSerializer.Deserialize<CaseBlock>(File.ReadAllText(p)) : null;
    }

    public IEnumerable<CaseBlock> ListBlocks(Guid caseId)
        => Directory.EnumerateFiles(BlockDir, "*.json")
            .Select(f => JsonSerializer.Deserialize<CaseBlock>(File.ReadAllText(f)))
            .Where(cb => cb is not null && cb!.CaseId == caseId)!
            .Select(cb => cb!);

    public void UpsertBlock(CaseBlock entity)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        var p = Path.Combine(BlockDir, entity.Id + ".json");
        File.WriteAllText(p, JsonSerializer.Serialize(entity, Opt));
    }

    // NOVÉ:
    public IEnumerable<Case> ListAll()
        => AllFiles()
           .Select(f => JsonSerializer.Deserialize<Case>(File.ReadAllText(f)))
           .Where(c => c is not null)!.Select(c => c!);

    public IEnumerable<Case> ListRecent(int take = 200)
        => Directory.EnumerateFiles(_dir, "*.json")
            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
            .Take(take)
            .Select(f => JsonSerializer.Deserialize<Case>(File.ReadAllText(f))!)
            .Where(c => c is not null);
}
