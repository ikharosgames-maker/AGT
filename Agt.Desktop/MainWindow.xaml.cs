using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;
using Agt.Desktop.Views;

namespace Agt.Desktop;

public partial class MainWindow : Window
{
    private bool _rubberSelecting;
    private Point _selStart;
    private readonly Brush _selFill = new SolidColorBrush(Color.FromArgb(0x22, 0x5F, 0xA8, 0xFA));
    private readonly Brush _selStroke = new SolidColorBrush(Color.FromArgb(0xFF, 0x5F, 0xA8, 0xFA));
    private readonly double _dockSnap = 16;

    private System.Collections.Generic.List<BlockControl> _clipboard = new();

    private DesignerViewModel VM => (DesignerViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        var api = new ApiClient("http://localhost:5000");
        DataContext = new DesignerViewModel(api);

        Loaded += async (_, __) =>
        {
            await VM.FormVM.LoadAsync(); // runtime náhled
        };
    }

    /* ===== Overlay panely: drag + „dock“ ke krajům ===== */
    private void Overlay_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb t) return;

        // bezpečné nalezení nadřízeného FrameworkElement (overlay border) přes vizuální strom
        DependencyObject current = t;
        FrameworkElement? overlay = null;
        while (current is not null)
        {
            if (current is FrameworkElement fe && fe.Name is "ToolboxOverlay" or "PropOverlay" or "LayersOverlay")
            {
                overlay = fe;
                break;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        if (overlay is null) return;

        var m = (Thickness)overlay.GetValue(MarginProperty);
        var next = new Thickness(m.Left + e.HorizontalChange, m.Top + e.VerticalChange, 0, 0);

        // „Dockování“ k okrajům okna
        if (System.Math.Abs(next.Left) < _dockSnap) next.Left = 0;
        if (System.Math.Abs(next.Top) < _dockSnap) next.Top = 0;
        if (System.Math.Abs((ActualWidth - (next.Left + overlay.RenderSize.Width)) - _dockSnap) < _dockSnap)
            next.Left = ActualWidth - overlay.RenderSize.Width - _dockSnap;
        if (System.Math.Abs((ActualHeight - (next.Top + overlay.RenderSize.Height)) - _dockSnap) < _dockSnap)
            next.Top = ActualHeight - overlay.RenderSize.Height - _dockSnap;

        overlay.Margin = next;
    }

    private void ToolboxMinimize_Click(object sender, RoutedEventArgs e)
        => ToolboxContent.Visibility = ToolboxContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private void PropMinimize_Click(object sender, RoutedEventArgs e)
        => PropContent.Visibility = PropContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    private void LayersMinimize_Click(object sender, RoutedEventArgs e)
        => LayersContent.Visibility = LayersContent.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

    /* ===== Canvas bounds (clamp) ===== */
    private void DesignCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        VM.CanvasWidth = DesignCanvas.ActualWidth;
        VM.CanvasHeight = DesignCanvas.ActualHeight;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // drž panely uvnitř okna
        ClampOverlay(ToolboxOverlay);
        ClampOverlay(PropOverlay);
        ClampOverlay(LayersOverlay);
    }
    private void ClampOverlay(FrameworkElement fe)
    {
        var m = (Thickness)fe.GetValue(MarginProperty);
        var left = System.Math.Max(0, System.Math.Min(m.Left, ActualWidth - fe.RenderSize.Width));
        var top = System.Math.Max(0, System.Math.Min(m.Top, ActualHeight - fe.RenderSize.Height));
        fe.Margin = new Thickness(left, top, 0, 0);
    }

    /* ===== Výběr / gumový výběr ===== */
    private void DesignCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(DesignCanvas);

        // Přidání prvku z toolboxu
        if (VM.SelectedToolboxItem is not null)
        {
            VM.AddControlAt(pos.X, pos.Y); // uvnitř dojde k deselect toolboxu
            return;
        }

        // start rubber-bandu
        _rubberSelecting = true;
        _selStart = pos;

        Canvas.SetLeft(SelectionRect, pos.X);
        Canvas.SetTop(SelectionRect, pos.Y);
        SelectionRect.Width = 0; SelectionRect.Height = 0;
        SelectionRect.Stroke = _selStroke; SelectionRect.Fill = _selFill;
        SelectionRect.Visibility = Visibility.Visible;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            foreach (var c in VM.Controls) c.IsSelected = false;
            VM.Selected = null;
        }
    }

    private void DesignCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_rubberSelecting || e.LeftButton != MouseButtonState.Pressed) return;
        var pos = e.GetPosition(DesignCanvas);

        var x = System.Math.Min(pos.X, _selStart.X);
        var y = System.Math.Min(pos.Y, _selStart.Y);
        var w = System.Math.Abs(pos.X - _selStart.X);
        var h = System.Math.Abs(pos.Y - _selStart.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }

    private void DesignCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_rubberSelecting)
        {
            _rubberSelecting = false;
            SelectionRect.Visibility = Visibility.Collapsed;

            var rect = new Rect(Canvas.GetLeft(SelectionRect), Canvas.GetTop(SelectionRect),
                                SelectionRect.Width, SelectionRect.Height);

            foreach (var c in VM.Controls)
            {
                var cRect = new Rect(c.X, c.Y, c.Width, c.Height);
                if (rect.IntersectsWith(cRect))
                {
                    c.IsSelected = true;
                    VM.Selected ??= c;
                }
            }
            VM.RefreshCommonCaps();
        }
    }

    /* ===== Klik na položku (výběr / Ctrl+toggle) ===== */
    private void Item_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var item = (sender as FrameworkElement)?.DataContext as BlockControl;
        if (item is null) return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            item.IsSelected = !item.IsSelected;
            if (item.IsSelected) VM.Selected = item;
            else if (VM.Selected == item) VM.Selected = VM.Controls.FirstOrDefault(c => c.IsSelected);
        }
        else
        {
            foreach (var c in VM.Controls) c.IsSelected = false;
            item.IsSelected = true;
            VM.Selected = item;
        }
        VM.RefreshCommonCaps();
        e.Handled = true;
    }

    /* ===== Před dragem na úchytu vyber „vlastníka“ úchytu ===== */
    private void Handle_SelectOwner(object sender, DragStartedEventArgs e)
    {
        var bc = (sender as FrameworkElement)?.DataContext as BlockControl;
        if (bc is null) return;

        if (!bc.IsSelected && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            foreach (var c in VM.Controls) c.IsSelected = false;
            bc.IsSelected = true;
            VM.Selected = bc;
            VM.RefreshCommonCaps();
        }
    }

    /* ===== Move/Resize ===== */
    private void Move_Handle_DragDelta(object sender, DragDeltaEventArgs e)
        => VM.MoveSelectedBy(e.HorizontalChange, e.VerticalChange);

    private void Resize_BottomRight(object sender, DragDeltaEventArgs e)
        => VM.ResizeSelected(e.HorizontalChange, e.VerticalChange, 0, 0);

    /* ===== ContextMenu ===== */
    private void Ctx_Delete(object sender, RoutedEventArgs e)
    {
        var selected = VM.Controls.Where(c => c.IsSelected).ToList();
        foreach (var c in selected) VM.Controls.Remove(c);
        VM.Selected = VM.Controls.FirstOrDefault();
        VM.RefreshCommonCaps();
    }

    private static BlockControl Clone(BlockControl s) => new()
    {
        ControlType = s.ControlType,
        X = s.X,
        Y = s.Y,
        Width = s.Width,
        Height = s.Height,
        Label = s.Label,
        DataPath = s.DataPath,
        Placeholder = s.Placeholder,
        IsReadOnly = s.IsReadOnly,
        IsMultiline = s.IsMultiline,
        ForegroundHex = s.ForegroundHex,
        BackgroundHex = s.BackgroundHex,
        FontSize = s.FontSize,
        Items = new System.Collections.Generic.List<string>(s.Items),
        SelectedItem = s.SelectedItem,
        HAlign = s.HAlign
    };

    private void Ctx_Copy(object sender, RoutedEventArgs e)
    {
        _clipboard = VM.Controls.Where(c => c.IsSelected).Select(Clone).ToList();
    }

    private void Ctx_Paste(object sender, RoutedEventArgs e)
    {
        foreach (var c in _clipboard.Select(Clone))
        {
            c.X += 16; c.Y += 16;
            VM.Controls.Add(c);
        }
    }

    private void Ctx_Duplicate(object sender, RoutedEventArgs e)
    {
        var first = VM.Selected;
        if (first is null) return;
        var c = Clone(first);
        c.X += 16; c.Y += 16;
        VM.Controls.Add(c);
        foreach (var it in VM.Controls) it.IsSelected = false;
        c.IsSelected = true;
        VM.Selected = c;
        VM.RefreshCommonCaps();
    }

    /* ===== Hromadné úpravy ===== */
    private void ApplyStyleToSelection_Click(object sender, RoutedEventArgs e)
    {
        if (VM.Selected is null) return;
        var src = VM.Selected;
        foreach (var c in VM.Controls.Where(c => c.IsSelected && !ReferenceEquals(c, src)))
        {
            if (VM.CommonCaps.ShowFont) c.FontSize = src.FontSize;
            if (VM.CommonCaps.ShowForeground) c.ForegroundHex = src.ForegroundHex;
            if (VM.CommonCaps.ShowBackground) c.BackgroundHex = src.BackgroundHex;
            if (VM.CommonCaps.ShowHAlign) c.HAlign = src.HAlign;
            if (VM.CommonCaps.ShowReadOnly) c.IsReadOnly = src.IsReadOnly;
        }
    }

    /* ===== Color picker tlačítka ===== */
    private void PickForeground_Click(object sender, RoutedEventArgs e)
    {
        var c = VM.Selected; if (c is null) return;
        var dlg = new ColorPickerWindow();
        if (dlg.ShowDialog() == true) c.ForegroundHex = dlg.SelectedHex;
    }

    private void PickBackground_Click(object sender, RoutedEventArgs e)
    {
        var c = VM.Selected; if (c is null) return;
        var dlg = new ColorPickerWindow();
        if (dlg.ShowDialog() == true) c.BackgroundHex = dlg.SelectedHex;
    }
}
