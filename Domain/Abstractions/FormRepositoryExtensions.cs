using System;
using System.Linq;
using System.Text.Json.Nodes;
using Agt.Domain.Models;

namespace Agt.Domain.Abstractions
{
    /// <summary>
    /// Convenience helpery nad IFormRepository pro práci s verzemi formulářů.
    /// Přizpůsobeno aktuálním DTO: Form, FormVersion, FormRepositoryEntry.
    /// </summary>
    public static class FormRepositoryExtensions
    {
        /// <summary>
        /// Načte konkrétní verzi podle formVersionId.
        /// Používá silně typovaný FormVersion.
        /// </summary>
        public static FormVersion? GetVersion(this IFormRepository repo, Guid formVersionId)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            return repo.GetVersion(formVersionId);
        }

        /// <summary>
        /// Vrátí poslední (nejvyšší) published verzi daného formuláře.
        /// </summary>
        public static FormRepositoryEntry? GetLatest(this IFormRepository repo, string formKey)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (string.IsNullOrWhiteSpace(formKey)) throw new ArgumentNullException(nameof(formKey));

            // Preferuj přímé API, pokud ho IFormRepository implementuje.
            try
            {
                return repo.GetLatest(formKey, includeDrafts: false);
            }
            catch (NotImplementedException)
            {
                // Fallback přes ListAll – kdyby někdo GetLatest neimplementoval.
                return repo.ListAll(formKey, includeDrafts: false)
                           .Where(e => !e.IsDraft)
                           .OrderByDescending(e => ParseVersion(e.Version))
                           .FirstOrDefault();
            }
        }

        /// <summary>
        /// Vrátí konkrétní verzi podle klíče a řetězcové verze (bez draftů).
        /// Dělá to přes existující index (FormRepositoryEntry + LoadJson).
        /// </summary>
        public static JsonNode? GetVersion(this IFormRepository repo, string formKey, string version)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (string.IsNullOrWhiteSpace(formKey)) throw new ArgumentNullException(nameof(formKey));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));

            var all = repo.ListAll(formKey, includeDrafts: true);
            var entry = all.FirstOrDefault(e =>
                !e.IsDraft &&
                string.Equals(e.Key, formKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Version, version, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
                return null;

            var (json, _) = repo.LoadJson(entry);
            return json;
        }

        /// <summary>
        /// Přidá novou verzi formuláře na základě stávajícího JSONu.
        /// Parametr <paramref name="version"/> je „base“/parent verze (může být null při prvním uložení).
        /// Signatura je přizpůsobená tomu, jak to volá FormProcessEditorViewModel
        /// (pojmenovaný parametr version).
        /// </summary>
        public static FormRepositoryEntry SaveNewVersion(
            this IFormRepository repo,
            string formKey,
            string? baseVersion,
            JsonNode editedJson,
            bool publish)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (string.IsNullOrWhiteSpace(formKey)) throw new ArgumentNullException(nameof(formKey));
            if (editedJson == null) throw new ArgumentNullException(nameof(editedJson));

            // Repozitář už poskytuje nízkoúrovňové API SaveNewVersion(formKey, baseVersion, json, publish).
            return repo.SaveNewVersion(formKey, baseVersion, editedJson, publish);
        }

        /// <summary>
        /// Nastaví konkrétní verzi jako published.
        /// Rozšiřuje existující IFormRepository.SetPublished(formKey, version).
        /// </summary>
        public static void SetPublished(this IFormRepository repo, string formKey, string version)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            if (string.IsNullOrWhiteSpace(formKey)) throw new ArgumentNullException(nameof(formKey));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentNullException(nameof(version));

            repo.SetPublished(formKey, version);
        }

        private static (int Major, int Minor, int Patch) ParseVersion(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return (0, 0, 0);

            var parts = v.TrimStart('v', 'V')
                         .Split('.', StringSplitOptions.RemoveEmptyEntries);

            int major = 0, minor = 0, patch = 0;
            if (parts.Length > 0) int.TryParse(parts[0], out major);
            if (parts.Length > 1) int.TryParse(parts[1], out minor);
            if (parts.Length > 2) int.TryParse(parts[2], out patch);

            return (major, minor, patch);
        }
        public static FormRepositoryEntry? Get(this IFormRepository repo, string key)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));
            return repo.Get(key);
        }

    }
}
