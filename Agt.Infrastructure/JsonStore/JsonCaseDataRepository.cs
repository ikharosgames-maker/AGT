using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore
{
    public sealed class JsonCaseDataRepository : ICaseDataRepository
    {
        private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public async Task<CaseDataSnapshot?> LoadAsync(Guid caseId)
        {
            var path = JsonPaths.CaseDataPath(caseId);
            if (!File.Exists(path)) return null;

            await using var s = File.OpenRead(path);
            var snapshot = await JsonSerializer.DeserializeAsync<CaseDataSnapshot>(s, _options);
            return snapshot;
        }

        public async Task SaveAsync(CaseDataSnapshot snapshot)
        {
            var path = JsonPaths.CaseDataPath(snapshot.CaseId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var s = File.Create(path);
            await JsonSerializer.SerializeAsync(s, snapshot, _options);
        }
    }
}
