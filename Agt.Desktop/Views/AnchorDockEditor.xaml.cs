using System.Windows;
using System.Windows.Controls;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public partial class AnchorDockEditor : UserControl
    {
        public AnchorDockEditor()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                RefreshAnchorButtons();
                RefreshDockButtons();
            };
        }

        // --- Dock ---
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register(nameof(Dock), typeof(DockTo), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(DockTo.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (o, e) => ((AnchorDockEditor)o).RefreshDockButtons()));

        public DockTo Dock
        {
            get => (DockTo)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }

        // --- Anchor (flags) ---
        public static readonly DependencyProperty AnchorProperty =
            DependencyProperty.Register(nameof(Anchor), typeof(AnchorSides), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(AnchorSides.Left | AnchorSides.Top, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (o, e) => ((AnchorDockEditor)o).RefreshAnchorButtons()));

        public AnchorSides Anchor
        {
            get => (AnchorSides)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        private void AnchorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            var a = Anchor;
            void Toggle(AnchorSides flag, bool on)
            {
                if (on) a |= flag;
                else a &= ~flag;
            }

            if (ReferenceEquals(sender, BtnTop))
                Toggle(AnchorSides.Top, BtnTop.IsChecked == true);
            else if (ReferenceEquals(sender, BtnBottom))
                Toggle(AnchorSides.Bottom, BtnBottom.IsChecked == true);
            else if (ReferenceEquals(sender, BtnLeft))
                Toggle(AnchorSides.Left, BtnLeft.IsChecked == true);
            else if (ReferenceEquals(sender, BtnRight))
                Toggle(AnchorSides.Right, BtnRight.IsChecked == true);

            Anchor = a;
            RefreshAnchorButtons();
        }

        private void RefreshAnchorButtons()
        {
            if (BtnTop == null) return;
            BtnTop.IsChecked = (Anchor & AnchorSides.Top) == AnchorSides.Top;
            BtnBottom.IsChecked = (Anchor & AnchorSides.Bottom) == AnchorSides.Bottom;
            BtnLeft.IsChecked = (Anchor & AnchorSides.Left) == AnchorSides.Left;
            BtnRight.IsChecked = (Anchor & AnchorSides.Right) == AnchorSides.Right;
        }

        private void DockToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (ReferenceEquals(sender, DockTop)) Dock = DockTo.Top;
            else if (ReferenceEquals(sender, DockBottom)) Dock = DockTo.Bottom;
            else if (ReferenceEquals(sender, DockLeft)) Dock = DockTo.Left;
            else if (ReferenceEquals(sender, DockRight)) Dock = DockTo.Right;
            else if (ReferenceEquals(sender, DockFill)) Dock = DockTo.Fill;

            RefreshDockButtons();
        }

        private void RefreshDockButtons()
        {
            if (DockTop == null) return;

            DockTop.IsChecked = Dock == DockTo.Top;
            DockBottom.IsChecked = Dock == DockTo.Bottom;
            DockLeft.IsChecked = Dock == DockTo.Left;
            DockRight.IsChecked = Dock == DockTo.Right;
            DockFill.IsChecked = Dock == DockTo.Fill;
        }
    }
}
