namespace Agt.Desktop.Models
{
    /// <summary>Popis položky v knihovně bloků.</summary>
    public sealed class BlockLibEntry
    {
        /// <summary>Identifikátor bloku (např. 'CustomerCard').</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Verze (např. '1.0').</summary>
        public string Version { get; set; } = "1.0";

        /// <summary>Titulek zobrazený v UI.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Plná cesta k JSON souboru s definicí bloku.</summary>
        public string FilePath { get; set; } = string.Empty;

        public override string ToString() => string.IsNullOrWhiteSpace(Title) ? $"{Key} ({Version})" : Title;
    }
}
