using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public partial class AnchorDockEditor : UserControl
    {
        public AnchorDockEditor()
        {
            InitializeComponent();
            Loaded += (_, __) => RefreshUI();
        }

        public AnchorSides Anchor
        {
            get => (AnchorSides)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }
        public static readonly DependencyProperty AnchorProperty =
            DependencyProperty.Register(nameof(Anchor), typeof(AnchorSides), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(AnchorSides.Left | AnchorSides.Top,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (_, __) => { }));

        public DockTo Dock
        {
            get => (DockTo)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register(nameof(Dock), typeof(DockTo), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(DockTo.None,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (_, __) => { }));

        private void RefreshUI()
        {
            BtnTop.IsChecked = Anchor.HasFlag(AnchorSides.Top);
            BtnBottom.IsChecked = Anchor.HasFlag(AnchorSides.Bottom);
            BtnLeft.IsChecked = Anchor.HasFlag(AnchorSides.Left);
            BtnRight.IsChecked = Anchor.HasFlag(AnchorSides.Right);
        }

        private void AnchorBtn_Click(object sender, RoutedEventArgs e)
        {
            AnchorSides a = 0;
            if (BtnLeft.IsChecked == true) a |= AnchorSides.Left;
            if (BtnTop.IsChecked == true) a |= AnchorSides.Top;
            if (BtnRight.IsChecked == true) a |= AnchorSides.Right;
            if (BtnBottom.IsChecked == true) a |= AnchorSides.Bottom;
            Anchor = a;
        }

        private void Dock_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is DockTo d)
                Dock = d;
        }
    }
}
