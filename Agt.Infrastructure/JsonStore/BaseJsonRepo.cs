using System.Text;
using System.Text.Json;

namespace Agt.Infrastructure.JsonStore;

// veřejná abstraktní třída (kvůli CS0060) a generické metody (kvůli CS0308)
public abstract class BaseJsonRepo
{
    protected readonly string _dir;
    protected static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };

    protected BaseJsonRepo(string subdir)
    {
        _dir = JsonPaths.Dir(subdir);
        // jistota, že cíl existuje (ať Save/Load nepadá)
        Directory.CreateDirectory(_dir);
    }

    protected string PathOf(Guid id) => Path.Combine(_dir, id + ".json");

    // ČTENÍ s lehkým retry (kvůli probíhající výměně souboru)
    protected async Task<T?> Load<T>(Guid id)
    {
        var p = PathOf(id);
        if (!File.Exists(p)) return default;

        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var json = await File.ReadAllTextAsync(p).ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(json, Opt);
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        // poslední pokus (nepohlcujeme výjimky navždy)
        var jsonFinal = await File.ReadAllTextAsync(p).ConfigureAwait(false);
        return JsonSerializer.Deserialize<T>(jsonFinal, Opt);
    }

    // ZÁPIS: temp soubor + atomická výměna (bez globálního locku)
    protected async Task Save<T>(Guid id, T obj)
    {
        var p = PathOf(id);
        Directory.CreateDirectory(_dir);

        var tmp = p + ".tmp";
        var json = JsonSerializer.Serialize(obj, Opt);

        await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 8192,
                                             FileOptions.Asynchronous | FileOptions.SequentialScan))
        await using (var sw = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            await sw.WriteAsync(json).ConfigureAwait(false);
            await sw.FlushAsync().ConfigureAwait(false);
            await fs.FlushAsync().ConfigureAwait(false);
        }

        try
        {
            if (File.Exists(p))
            {
                // Bez pojmenovaných parametrů – funguje na všech podporovaných CS/targetech
                File.Replace(tmp, p, null, true);
            }
            else
            {
                File.Move(tmp, p);
            }
        }
        catch
        {
            // Fallback: zkusíme „delete+move“
            try { if (File.Exists(p)) File.Delete(p); } catch { /* ignore */ }
            File.Move(tmp, p);
        }
    }


    protected IEnumerable<string> AllFiles() => Directory.EnumerateFiles(_dir, "*.json");
}
