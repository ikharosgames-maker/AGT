using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;

namespace Agt.Infrastructure.JsonStore
{
    /// <summary>
    /// Souborový JSON repozitář pro formuláře.
    /// - Index verzí je v jednom souboru forms.index.json (FormRepositoryEntry).
    /// - Každá verze má vlastní soubor (serializovaný FormVersion),
    ///   kde StageGraphJson obsahuje celý layout z editoru.
    /// </summary>
    public sealed class JsonFormRepository : IFormRepository
    {
        private readonly string _formsRoot;
        private readonly string _versionsRoot;
        private readonly string _indexPath;

        private static readonly JsonSerializerOptions Opt = new()
        {
            WriteIndented = true
        };

        public JsonFormRepository()
        {
            _formsRoot = JsonPaths.Dir("forms");
            _versionsRoot = JsonPaths.Dir("form-versions");
            _indexPath = Path.Combine(_formsRoot, "forms.index.json");

            Directory.CreateDirectory(_formsRoot);
            Directory.CreateDirectory(_versionsRoot);
        }

        // =====================================================================
        //  Interní práce s indexem (FormRepositoryEntry)
        // =====================================================================

        private List<FormRepositoryEntry> LoadIndex()
        {
            if (!File.Exists(_indexPath))
                return new List<FormRepositoryEntry>();

            var json = File.ReadAllText(_indexPath);
            var list = JsonSerializer.Deserialize<List<FormRepositoryEntry>>(json, Opt);
            return list ?? new List<FormRepositoryEntry>();
        }

        private void SaveIndex(List<FormRepositoryEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries, Opt);
            File.WriteAllText(_indexPath, json);
        }

        private static (int Major, int Minor, int Patch) ParseVersion(string? v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return (0, 0, 0);

            var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
            int major = 0, minor = 0, patch = 0;

            if (parts.Length > 0) int.TryParse(parts[0], out major);
            if (parts.Length > 1) int.TryParse(parts[1], out minor);
            if (parts.Length > 2) int.TryParse(parts[2], out patch);

            return (major, minor, patch);
        }

        private static string FormatVersion((int Major, int Minor, int Patch) v)
            => $"{v.Major}.{v.Minor}.{v.Patch}";

        private static (int Major, int Minor, int Patch) NextMinor((int Major, int Minor, int Patch) v)
            => (v.Major, v.Minor + 1, 0);

        private string GetVersionFile(string formKey, string version)
            => Path.Combine(_versionsRoot, $"{formKey}__{version}.json");

        // =====================================================================
        //  IFormRepository – dotazy
        // =====================================================================

        public IEnumerable<FormRepositoryEntry> ListAll(string? keyFilter, bool includeDrafts)
        {
            var idx = LoadIndex().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(keyFilter))
                idx = idx.Where(e => string.Equals(e.Key, keyFilter, StringComparison.OrdinalIgnoreCase));

            if (!includeDrafts)
                idx = idx.Where(e => !e.IsDraft);

            return idx
                .OrderBy(e => e.Key)
                .ThenBy(e => ParseVersion(e.Version))
                .ToList();
        }

        public FormRepositoryEntry? GetLatest(string formKey, bool includeDrafts)
        {
            var idx = ListAll(formKey, includeDrafts);
            return idx
                .OrderByDescending(e => ParseVersion(e.Version))
                .FirstOrDefault();
        }

        /// <summary>
        /// DOPLNĚNO – kvůli volání forms.Get(fv.FormId) v UI.
        /// Projde index, načte FormVersion a hledá shodu na FormId.
        /// Vrací zatím první nalezený záznam (typicky poslední uložená verze daného FormId).
        /// </summary>
        public FormRepositoryEntry? Get(Guid id)
        {
            var index = LoadIndex();

            foreach (var entry in index)
            {
                var (_, fv) = LoadJson(entry);
                if (fv != null && fv.FormId == id)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))
                        entry.Name = entry.Key; // defaultně použijeme Key jako Name

                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// DOPLNĚNO – Get podle formKey (např. pro seznamy formulářů).
        /// Vrací poslední verzi, včetně draftů.
        /// </summary>
        public FormRepositoryEntry? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            return ListAll(key, includeDrafts: true)
                .OrderByDescending(e => ParseVersion(e.Version))
                .FirstOrDefault();
        }

        public (JsonNode? Json, FormVersion? Version) LoadJson(FormRepositoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var path = entry.StoragePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(_versionsRoot, path);

            if (!File.Exists(path))
                return (null, null);

            var txt = File.ReadAllText(path);
            var fv = JsonSerializer.Deserialize<FormVersion>(txt, Opt);

            JsonNode? layout = null;
            if (fv != null && !string.IsNullOrWhiteSpace(fv.StageGraphJson))
            {
                try
                {
                    layout = JsonNode.Parse(fv.StageGraphJson);
                }
                catch
                {
                    // rozbitý layout necháme jako null, ale metadata vrátíme
                }
            }

            return (layout, fv);
        }

        public FormVersion? GetVersion(Guid id)
        {
            var idx = LoadIndex();
            var entry = idx.FirstOrDefault(e => e.Id == id);
            if (entry == null)
                return null;

            var path = entry.StoragePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(_versionsRoot, path);

            if (!File.Exists(path))
                return null;

            var txt = File.ReadAllText(path);
            return JsonSerializer.Deserialize<FormVersion>(txt, Opt);
        }

        public FormVersion? GetVersion(string key, string version)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException(nameof(version));

            var idx = LoadIndex();
            var entry = idx.FirstOrDefault(e =>
                string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return null;

            return GetVersion(entry.Id);
        }

        // =====================================================================
        //  IFormRepository – zápisy
        // =====================================================================

        public void Upsert(Form form)
        {
            // V současné verzi se Form jako samostatný objekt nepersistuje.
            // Metoda je připravená pro budoucí rozšíření (SQL apod.).
            throw new NotImplementedException();
        }

        public void UpsertVersion(FormVersion version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            var index = LoadIndex();
            var entry = index.FirstOrDefault(e => e.Id == version.Id);
            if (entry == null)
                throw new InvalidOperationException($"FormVersion {version.Id:D} not found in index.");

            var path = entry.StoragePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(_versionsRoot, path);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(version, Opt));
        }

        public FormVersion SaveVersion(string key, string? parentVersion, JsonNode jsonNode, bool isDraft)
        {
            var entry = SaveVersionInternal(key, parentVersion, jsonNode, isDraft);
            var fv = GetVersion(entry.Id);
            if (fv == null)
                throw new InvalidOperationException($"Failed to load FormVersion {entry.Id:D} after save.");
            return fv;
        }

        private FormRepositoryEntry SaveVersionInternal(
            string formKey,
            string? baseVersion,
            JsonNode editedJson,
            bool isDraft)
        {
            if (string.IsNullOrWhiteSpace(formKey))
                throw new ArgumentNullException(nameof(formKey));
            if (editedJson == null)
                throw new ArgumentNullException(nameof(editedJson));

            var index = LoadIndex();

            // Z čeho počítat novou verzi?
            (int Major, int Minor, int Patch) current;
            if (!string.IsNullOrWhiteSpace(baseVersion))
            {
                current = ParseVersion(baseVersion);
            }
            else
            {
                var last = index
                    .Where(e => string.Equals(e.Key, formKey, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => ParseVersion(e.Version))
                    .FirstOrDefault();

                current = last != null
                    ? ParseVersion(last.Version)
                    : (1, 0, 0);
            }

            var next = NextMinor(current);
            var newVersion = FormatVersion(next);

            var fv = new FormVersion
            {
                Id = Guid.NewGuid(),
                FormId = Guid.NewGuid(),
                Version = newVersion,
                Status = FormStatus.Draft,
                ChangeLog = string.Empty,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                BlockPinsJson = "{}",
                StageGraphJson = editedJson.ToJsonString()
            };

            var fileName = $"{formKey}__{newVersion}.json";
            var fullPath = GetVersionFile(formKey, newVersion);
            Directory.CreateDirectory(_versionsRoot);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(fv, Opt));

            var entry = new FormRepositoryEntry
            {
                Id = fv.Id,              // zde Id = FormVersionId
                Key = formKey,
                Name = formKey,          // DOPLNĚNO – defaultně klíč jako název
                Version = newVersion,
                IsDraft = isDraft,
                StoragePath = fileName
            };

            index.RemoveAll(e => e.Id == entry.Id && e.Version == entry.Version);
            index.Add(entry);
            SaveIndex(index);

            return entry;
        }

        public void SetPublished(string formKey, string version)
        {
            if (string.IsNullOrWhiteSpace(formKey)) throw new ArgumentNullException(nameof(formKey));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));

            var index = LoadIndex();

            var target = index.FirstOrDefault(e =>
                string.Equals(e.Key, formKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));

            if (target != null)
            {
                target.IsDraft = false;
                SaveIndex(index);
            }
        }

        public FormRepositoryEntry SaveNewVersion(
            string formKey,
            string? baseVersion,
            JsonNode editedJson,
            bool publish)
        {
            var entry = SaveVersionInternal(formKey, baseVersion, editedJson, isDraft: !publish);

            if (publish)
            {
                SetPublished(formKey, entry.Version);
            }

            return entry;
        }
    }
}
