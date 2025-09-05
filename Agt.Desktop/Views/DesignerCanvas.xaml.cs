using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Agt.Desktop.Models;
using Agt.Desktop.Services;
using Agt.Desktop.ViewModels;
using Agt.Desktop.Views.Adorners;

namespace Agt.Desktop.Views
{
    public partial class DesignerCanvas : UserControl
    {
        private SelectionService _selection => (SelectionService)Application.Current.Resources["SelectionService"];
        private SelectionToolbarAdorner? _toolbarAdorner;

        public DesignerCanvas()
        {
            InitializeComponent();
        }

        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var item = fe?.DataContext;
            if (item == null) return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                _selection.Toggle(item);
            else
                _selection.SelectSingle(item);

            e.Handled = true;
            UpdateFloatingBar();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender)
            {
                _selection.Clear();
                UpdateFloatingBar();
            }
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            UpdateFloatingBar();
        }

        private void RootCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("field/key")) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void RootCanvas_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("field/key")) return;
            var key = (string)e.Data.GetData("field/key")!;
            var pos = e.GetPosition((IInputElement)sender);

            if (DataContext is DesignerViewModel vm)
                vm.CreateFromLibrary(key, pos);

            e.Handled = true;
            UpdateFloatingBar();
        }

        private void UpdateFloatingBar()
        {
            var layer = AdornerLayer.GetAdornerLayer(this);
            if (layer == null) return;

            if (_selection.Count == 0)
            {
                if (_toolbarAdorner != null) { layer.Remove(_toolbarAdorner); _toolbarAdorner = null; }
                return;
            }

            var rect = GetSelectionBounds();
            if (_toolbarAdorner == null)
            {
                _toolbarAdorner = new SelectionToolbarAdorner(this, DataContext!);
                layer.Add(_toolbarAdorner);
            }
            _toolbarAdorner.UpdateBounds(rect);
        }

        private Rect GetSelectionBounds()
        {
            var items = _selection.SelectedItems.Cast<FieldComponentBase>().ToList();
            if (items.Count == 0) return new Rect(0, 0, 0, 0);

            double minX = items.Min(i => i.X);
            double minY = items.Min(i => i.Y);
            double maxX = items.Max(i => i.X + i.Width);
            double maxY = items.Max(i => i.Y + i.Height);

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }
    }
}
