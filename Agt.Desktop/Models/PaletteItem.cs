using System;

namespace Agt.Desktop.Models
{
    /// <summary>
    /// Minimální podoba položky v paletě pro editor (odpovídá tomu, co čte code-behind).
    /// </summary>
    public sealed class PaletteItem
    {
        public Guid BlockId { get; set; }
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
    }
}