using System.Text.Json;

namespace Agt.Infrastructure.JsonStore;

// veřejná abstraktní třída (kvůli CS0060) a generické metody (kvůli CS0308)
public abstract class BaseJsonRepo
{
    protected readonly string _dir;
    protected static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };
    protected static readonly SemaphoreSlim Gate = new(1, 1);

    protected BaseJsonRepo(string subdir) => _dir = JsonPaths.Dir(subdir);

    protected string PathOf(Guid id) => Path.Combine(_dir, id + ".json");

    protected async Task<T?> Load<T>(Guid id)
    {
        var p = PathOf(id);
        if (!File.Exists(p)) return default;
        await Gate.WaitAsync();
        try { return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(p), Opt); }
        finally { Gate.Release(); }
    }

    protected async Task Save<T>(Guid id, T obj)
    {
        var p = PathOf(id);
        await Gate.WaitAsync();
        try { await File.WriteAllTextAsync(p, JsonSerializer.Serialize(obj, Opt)); }
        finally { Gate.Release(); }
    }

    protected IEnumerable<string> AllFiles() => Directory.EnumerateFiles(_dir, "*.json");
}
