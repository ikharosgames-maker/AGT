using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Agt.Domain.Models;
using Agt.Domain.Repositories;

namespace Agt.Infrastructure.JsonStore
{
    /// <summary>JSON implementace uložení dat case. Soubor: case-data/{CaseId}.json</summary>
    public class JsonCaseDataRepository : ICaseDataRepository
    {
        private readonly string _dir;
        private static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };

        public JsonCaseDataRepository()
        {
            _dir = JsonPaths.Dir("case-data");
            Directory.CreateDirectory(_dir);
        }

        private string PathOf(Guid id) => System.IO.Path.Combine(_dir, id + ".json");

        public async Task<CaseDataSnapshot?> LoadAsync(Guid caseId)
        {
            var p = PathOf(caseId);
            if (!File.Exists(p)) return null;

            using var fs = File.Open(p, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<CaseDataSnapshot>(fs, Opt).ConfigureAwait(false);
        }

        public async Task SaveAsync(CaseDataSnapshot snapshot)
        {
            snapshot.UpdatedAt = DateTime.UtcNow;

            var p = PathOf(snapshot.CaseId);
            var tmp = p + ".tmp";

            var json = JsonSerializer.Serialize(snapshot, Opt);
            await File.WriteAllTextAsync(tmp, json).ConfigureAwait(false);

            if (File.Exists(p))
                File.Replace(tmp, p, null, true);
            else
                File.Move(tmp, p);
        }
    }
}
