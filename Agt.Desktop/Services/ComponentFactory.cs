using System.Collections.ObjectModel;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Dočasná "no-op" továrna: nezkouší instancovat FieldComponentBase (abstraktní)
    /// ani sahat na vlastnosti, které nemusí existovat. Vrací prázdnou kolekci,
    /// aby se projekt bez chyb zkompiloval a běžel.
    ///
    /// Až pošleš zdroj dat (DTO/JSON) a skutečné typy potomků, doplním Create(...) tak,
    /// aby vracela vaše konkrétní Field* typy a doplnila jim hodnoty (Title/Value/Options...).
    /// </summary>
    public static class ComponentFactory
    {
        // Minimální popisy – ponechávám pro budoucí mapování
        public sealed class FieldDescriptor
        {
            public string? Type { get; set; }
            public string? Key { get; set; }
            public string? Title { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; } = 160;
            public double Height { get; set; } = 40;
            public object? Default { get; set; }
            public System.Collections.Generic.IEnumerable<string>? Options { get; set; }
        }

        public sealed class BlockDescriptor
        {
            public string? Key { get; set; }
            public string? Title { get; set; }
            public string? Version { get; set; }
            public double? PreviewWidth { get; set; }
            public double? PreviewHeight { get; set; }
            public System.Collections.Generic.List<FieldDescriptor> Fields { get; set; } = new();
        }

        /// <summary>
        /// Dočasně vrací prázdnou kolekci – nic nerozbije, build projde.
        /// </summary>
        public static ObservableCollection<FieldComponentBase> BuildFrom(BlockDescriptor _)
            => new ObservableCollection<FieldComponentBase>();
    }
}
