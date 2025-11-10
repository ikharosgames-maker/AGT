using System.Runtime.InteropServices;

namespace Agt.Infrastructure.JsonStore;

public static class JsonPaths
{
    internal static string Root
    {
        get
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var root = Path.Combine(baseDir, "AGT");
            Directory.CreateDirectory(root);
            return root;
        }
    }

    public static string Dir(string name)
    {
        var d = Path.Combine(Root, name);
        Directory.CreateDirectory(d);
        return d;
    }
}
