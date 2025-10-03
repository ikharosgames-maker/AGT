using System.Collections.Generic;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    /// <summary>Knihovna bloků na disku (JSON soubory). Slouží jako katalog do palety procesního editoru.</summary>
    public interface IBlockLibrary
    {
        /// <summary>Adresář knihovny (jen ke čtení – nastavuje implementace).</summary>
        string LibraryRoot { get; }

        /// <summary>Uloží definici bloku do knihovny (přepíše existující se stejným Key+Version).</summary>
        void SaveToLibrary(object blockDto);

        /// <summary>Vrátí přehled dostupných bloků: Key, Name/Title, Version a cesta k JSON.</summary>
        IEnumerable<BlockCatalogItem> Enumerate();

        /// <summary>Zkusí načíst raw JSON daného souboru (pro případ náhledu).</summary>
        string LoadRawJson(string filePath);

        /// <summary>Najde a načte JSON bloku podle Key + Version.</summary>
        bool TryLoadByKeyVersion(string key, string version, out JsonDocument? document, out string? filePath);
    }

    public sealed class BlockCatalogItem
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public string Version { get; init; } = "";
        public string FilePath { get; init; } = "";
        public override string ToString() => $"{Name} [{Key}] v{Version}";
    }
}
