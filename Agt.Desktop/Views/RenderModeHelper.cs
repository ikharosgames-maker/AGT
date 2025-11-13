using System.Windows;
using Agt.Desktop.Models; // <- důležité

namespace Agt.Desktop.Views
{
    public static class RenderModeHelper
    {
        public static RenderMode GetMode(DependencyObject obj)
            => (RenderMode)obj.GetValue(ModeProperty);

        public static void SetMode(DependencyObject obj, RenderMode value)
            => obj.SetValue(ModeProperty, value);

        // Jediný zdroj pravdy: Agt.Desktop.Models.RenderMode
        // Inherits zajistí dědění režimu stromem vizuálu.
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(RenderMode),            // <- enum z Models
                typeof(RenderModeHelper),
                new FrameworkPropertyMetadata(RenderMode.Run, FrameworkPropertyMetadataOptions.Inherits));
    }
}
