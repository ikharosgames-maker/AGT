using System;

namespace Agt.Desktop.Models
{
    public sealed class PaletteItem
    {
        public Guid BlockId { get; set; }
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
    }
}
