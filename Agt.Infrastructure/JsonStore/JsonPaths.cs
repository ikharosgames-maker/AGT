using System;
using System.IO;

namespace Agt.Infrastructure.JsonStore
{
    public static class JsonPaths
    {
        private static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AGT");

        public static string BaseDirectory => BaseDir;

        public static string EnsureDir(string relative)
        {
            var full = Path.Combine(BaseDir, relative);
            Directory.CreateDirectory(full);
            return full;
        }

        public static string Dir(string relative) => EnsureDir(relative);

        public static string CaseDataDir => EnsureDir("case-data");

        public static string CaseDataPath(Guid caseId)
            => Path.Combine(CaseDataDir, $"{caseId:N}.json");
    }
}
