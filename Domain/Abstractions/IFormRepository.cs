using System.Text.Json.Nodes;
using Agt.Domain.Models;

namespace Agt.Domain.Abstractions
{
    public interface IFormRepository
    {
        IEnumerable<FormRepositoryEntry> ListAll(string? keyPrefix = null, bool includeDrafts = true);

        FormRepositoryEntry? GetLatest(string key, bool includeDrafts);

        // DOPLNĚNO – kvůli existujícímu kódu v UI
        FormRepositoryEntry? Get(Guid id);

        // DOPLNĚNO – kvůli voláním podle formKey
        FormRepositoryEntry? Get(string key);

        FormVersion? GetVersion(Guid id);

        FormVersion? GetVersion(string key, string version);

        (JsonNode? Json, FormVersion? Version) LoadJson(FormRepositoryEntry entry);

        void Upsert(Form form);

        void UpsertVersion(FormVersion version);

        FormVersion SaveVersion(string key, string? parentVersion, JsonNode jsonNode, bool isDraft);

        FormRepositoryEntry SaveNewVersion(string key, string? baseVersion, JsonNode jsonNode, bool publish);

        void SetPublished(string key, string version);
    }
}
