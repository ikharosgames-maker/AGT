using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Agt.Desktop.Services;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas
    {
        private readonly HashSet<object> _hooked = new();

        private void AnchorDock_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.LayoutUpdated += DesignerCanvas_LayoutUpdated;
            ApplyAll();
        }

        private void AnchorDock_OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.LayoutUpdated -= DesignerCanvas_LayoutUpdated;
            foreach (var fe in EnumerateItemElements())
                if (fe.DataContext is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged -= Item_PropertyChanged;
            _hooked.Clear();
        }

        private void AnchorDock_OnSizeChanged(object sender, SizeChangedEventArgs e) => ApplyAll();

        private void DesignerCanvas_LayoutUpdated(object? sender, System.EventArgs e)
        {
            foreach (var fe in EnumerateItemElements())
            {
                var ctx = fe.DataContext;
                if (ctx == null || _hooked.Contains(ctx)) continue;

                if (ctx is INotifyPropertyChanged inpc)
                    inpc.PropertyChanged += Item_PropertyChanged;

                _hooked.Add(ctx);
                AnchorDockService.ResetBaseline(ctx, new Size(ActualWidth, ActualHeight));
                AnchorDockService.Apply(ctx, new Size(ActualWidth, ActualHeight));
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ShouldSuspendAnchorDock()) return;

            if (sender == null) return;
            switch (e.PropertyName)
            {
                case "Anchor": // změna flags
                    AnchorDockService.ResetBaseline(sender, new Size(ActualWidth, ActualHeight));
                    AnchorDockService.Apply(sender, new Size(ActualWidth, ActualHeight));
                    break;

                case "Dock":
                case "X":
                case "Y":
                case "Width":
                case "Height":
                    AnchorDockService.Apply(sender, new Size(ActualWidth, ActualHeight));
                    break;
            }
        }

        private void ApplyAll()
        {
            if (ShouldSuspendAnchorDock()) return;

            var parentSize = new Size(ActualWidth, ActualHeight);
            foreach (var fe in EnumerateItemElements())
            {
                var ctx = fe.DataContext;
                if (ctx != null)
                    AnchorDockService.Apply(ctx, parentSize);
            }
        }

        private IEnumerable<FrameworkElement> EnumerateItemElements()
        {
            return FindVisualChildren<FrameworkElement>(this)
                   .Where(fe => fe.DataContext != null &&
                                fe.DataContext.GetType().GetProperty("X") != null &&
                                fe.DataContext.GetType().GetProperty("Y") != null &&
                                fe.DataContext.GetType().GetProperty("Width") != null &&
                                fe.DataContext.GetType().GetProperty("Height") != null);
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject d) where T : DependencyObject
        {
            if (d == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                if (child is T t) yield return t;
                foreach (var c in FindVisualChildren<T>(child))
                    yield return c;
            }
        }
    }
}
