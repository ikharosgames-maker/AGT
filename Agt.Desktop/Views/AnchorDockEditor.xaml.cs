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
            Loaded += (_, __) => RefreshDockButtons();
        }

        #region DependencyProperty Dock (enum DockTo)
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.Register(nameof(Dock), typeof(DockTo), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(DockTo.None, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (o, e) => ((AnchorDockEditor)o).RefreshDockButtons()));

        public DockTo Dock
        {
            get => (DockTo)GetValue(DockProperty);
            set => SetValue(DockProperty, value);
        }
        #endregion

        #region DependencyProperty Anchor (ponecháno – pokud ho používáš)
        public object? Anchor
        {
            get => GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        public static readonly DependencyProperty AnchorProperty =
            DependencyProperty.Register(nameof(Anchor), typeof(object), typeof(AnchorDockEditor),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        #endregion

        // Anchor kliky – nechávám prázdné, ať nepřepisuju tvoji logiku
        private void AnchorBtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO: tvá stávající implementace (pokud nějaká) – tady nechávám beze změn
        }

        // Dock: všechna tlačítka jsou ToggleButtony, ale chováme se jako „radio“ (tj. vždy jen jedno zapnuté)
        private void DockToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            if (ReferenceEquals(sender, DockTop)) Dock = DockTo.Top;
            else if (ReferenceEquals(sender, DockBottom)) Dock = DockTo.Bottom;
            else if (ReferenceEquals(sender, DockLeft)) Dock = DockTo.Left;
            else if (ReferenceEquals(sender, DockRight)) Dock = DockTo.Right;
            else if (ReferenceEquals(sender, DockFill)) Dock = DockTo.Fill;
            else if (ReferenceEquals(sender, DockNone)) Dock = DockTo.None;

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
            DockNone.IsChecked = Dock == DockTo.None;
        }
    }
}
