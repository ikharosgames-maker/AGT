using System.Windows;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public static class RenderModeHelper
    {
        // Inherited DP, ať se přenese z hostitele (Viewer/Canvas) do šablon
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(RenderMode),
                typeof(RenderModeHelper),
                new FrameworkPropertyMetadata(RenderMode.ReadOnly, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetMode(DependencyObject element, RenderMode value) =>
            element.SetValue(ModeProperty, value);

        public static RenderMode GetMode(DependencyObject element) =>
            (RenderMode)element.GetValue(ModeProperty);
    }
}
