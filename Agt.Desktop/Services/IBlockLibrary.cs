using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Agt.Desktop.Services
{
    public sealed record BlockLibEntry(Guid BlockId, string Key, string Name, string Version, string FilePath);

    public interface IBlockLibrary : IDisposable
    {
        IEnumerable<BlockLibEntry> Enumerate();

        // KANON: načítání výhradně přes BlockId + Version
        bool TryLoadByIdVersion(Guid blockId, string version,
                                out JsonDocument? doc, out BlockLibEntry? entry);

        // KANON: ukládání výhradně přes BlockId + Version
        bool SaveToLibrary(Guid blockId, string version, JsonElement schemaRoot,
                           string? key = null, string? blockName = null);
    }
}
