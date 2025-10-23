using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Agt.Desktop.Services
{
    public enum VersionBump { Patch, Minor, Major }

    public interface IFormVersioningService
    {
        string ComputeNext(string currentVersion, VersionBump bump);
        string ComputeNextFree(string formsRoot, string key, string currentVersion, VersionBump bump);
    }

    public sealed class FormVersioningService : IFormVersioningService
    {
        private static readonly Regex SemVerRx =
            new(@"^(?<maj>\d+)\.(?<min>\d+)\.(?<pat>\d+)$", RegexOptions.Compiled);

        public string ComputeNext(string currentVersion, VersionBump bump)
        {
            if (string.IsNullOrWhiteSpace(currentVersion) ||
                !SemVerRx.IsMatch(currentVersion))
            {
                // fallback: když to není semver, začni od 1.0.0
                return "1.0.0";
            }

            var m = SemVerRx.Match(currentVersion);
            int maj = int.Parse(m.Groups["maj"].Value);
            int min = int.Parse(m.Groups["min"].Value);
            int pat = int.Parse(m.Groups["pat"].Value);

            switch (bump)
            {
                case VersionBump.Major: maj++; min = 0; pat = 0; break;
                case VersionBump.Minor: min++; pat = 0; break;
                default: pat++; break;
            }
            return $"{maj}.{min}.{pat}";
        }

        public string ComputeNextFree(string formsRoot, string key, string currentVersion, VersionBump bump)
        {
            string next = ComputeNext(currentVersion, bump);
            string San(string s) => string.Join("_", s.Split(Path.GetInvalidFileNameChars()));

            while (File.Exists(Path.Combine(formsRoot, $"{San(key)}__{San(next)}.json")))
            {
                // když existuje, přidávej PATCH
                var m = SemVerRx.Match(next);
                if (m.Success)
                {
                    int maj = int.Parse(m.Groups["maj"].Value);
                    int min = int.Parse(m.Groups["min"].Value);
                    int pat = int.Parse(m.Groups["pat"].Value) + 1;
                    next = $"{maj}.{min}.{pat}";
                }
                else
                {
                    // non-semver: přidej suffix -copyN
                    next = next + "-copy";
                }
            }
            return next;
        }
    }
}
