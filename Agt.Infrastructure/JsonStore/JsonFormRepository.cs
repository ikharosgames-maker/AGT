using System.Text.Json;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore;

public sealed class JsonFormRepository : BaseJsonRepo, IFormRepository
{
    public JsonFormRepository() : base("forms") { }

    public Form? Get(Guid id) => Load<Form>(id).GetAwaiter().GetResult();

    public FormVersion? GetVersion(Guid id)
    {
        var dir = JsonPaths.Dir("form-versions");
        var path = Path.Combine(dir, id.ToString("D") + ".json");

        // 1) autoritativně: file podle GUID názvu
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            // můžeš deserializovat přímo do FormVersion
            return JsonSerializer.Deserialize<FormVersion>(json);
        }

        // 2) (volitelné) tvrdé odmítnutí – ať hned zjistíš, že chybí soubor
        // Pokud nechceš ani minimální scan, tuhle část vynech
        throw new FileNotFoundException($"FormVersion file not found: {path}");
    }

    public IEnumerable<FormVersion> ListVersions(Guid formId)
        => AllFiles()
           .Select(f => JsonSerializer.Deserialize<FormVersion>(File.ReadAllText(f)))
           .Where(v => v is not null && v!.FormId == formId)!
           .Select(v => v!);

    public void Upsert(Form form)
    {
        if (form.Id == Guid.Empty) form.Id = Guid.NewGuid();
        Save(form.Id, form).GetAwaiter().GetResult();
    }

    public void UpsertVersion(FormVersion version)
    {
        if (version.Id == Guid.Empty) version.Id = Guid.NewGuid();
        Save(version.Id, version).GetAwaiter().GetResult();
    }
}
