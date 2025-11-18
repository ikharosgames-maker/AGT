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
            Loaded += (_, __) =>
            {
                RefreshAnchorButtons();
                RefreshDockButtons();
            };
        }

        // ===== Dependency Properties =====

        public static readonly DependencyProperty AnchorProperty =
            DependencyProperty.Register(
                nameof(Anchor),
                typeof(AnchorSides),
                typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(
                    AnchorSides.Left | AnchorSides.Top,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnAnchorChanged));

        public AnchorSides Anchor
        {
            get => (AnchorSides)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        private static void OnAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (AnchorDockEditor)d;
            ctl.RefreshAnchorButtons();
        }

        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register(
                nameof(Dock),
                typeof(DockTo),
                typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(
                    DockTo.None,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDockChanged));

        public DockTo Dock
        {
            get => (DockTo)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }

        private static void OnDockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctl = (AnchorDockEditor)d;
            ctl.RefreshDockButtons();
        }

        // ===== Anchor UI =====

        private void AnchorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton btn) return;

            var a = Anchor;

            if (btn == BtnLeft) a ^= AnchorSides.Left;
            if (btn == BtnTop) a ^= AnchorSides.Top;
            if (btn == BtnRight) a ^= AnchorSides.Right;
            if (btn == BtnBottom) a ^= AnchorSides.Bottom;


            Anchor = a;
            RefreshAnchorButtons();
        }

        private void RefreshAnchorButtons()
        {
            BtnLeft.IsChecked = (Anchor & AnchorSides.Left) == AnchorSides.Left;
            BtnTop.IsChecked = (Anchor & AnchorSides.Top) == AnchorSides.Top;
            BtnRight.IsChecked = (Anchor & AnchorSides.Right) == AnchorSides.Right;
            BtnBottom.IsChecked = (Anchor & AnchorSides.Bottom) == AnchorSides.Bottom;
        }

        // ===== Dock UI =====

        private void DockToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton btn) return;

            var requested = DockTo.None;
            if (btn == DockLeft) requested = DockTo.Left;
            else if (btn == DockTop) requested = DockTo.Top;
            else if (btn == DockRight) requested = DockTo.Right;
            else if (btn == DockBottom) requested = DockTo.Bottom;
            else if (btn == DockFill) requested = DockTo.Fill;

            // opakovaný klik na aktivní = vypnout (None)
            Dock = (Dock == requested) ? DockTo.None : requested;

            RefreshDockButtons();
        }

        private void RefreshDockButtons()
        {
            // exkluzivita & vizuální stav
            DockLeft.IsChecked = Dock == DockTo.Left;
            DockTop.IsChecked = Dock == DockTo.Top;
            DockRight.IsChecked = Dock == DockTo.Right;
            DockBottom.IsChecked = Dock == DockTo.Bottom;
            DockFill.IsChecked = Dock == DockTo.Fill;
        }
    }
}
