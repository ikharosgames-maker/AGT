using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public partial class ComponentViewerControl : UserControl
    {
        public ComponentViewerControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items),
                typeof(IEnumerable),
                typeof(ComponentViewerControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None)
            );

        public IEnumerable Items
        {
            get => (IEnumerable)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.RegisterAttached(
                "Mode",
                typeof(RenderMode),
                typeof(ComponentViewerControl),
                new FrameworkPropertyMetadata(RenderMode.ReadOnly, FrameworkPropertyMetadataOptions.Inherits)
            );

        public RenderMode Mode
        {
            get => (RenderMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
    }
}
