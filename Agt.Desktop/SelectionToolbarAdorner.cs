using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Agt.Desktop.Views.Adorners
{
    public class SelectionToolbarAdorner : Adorner
    {
        private readonly VisualCollection _visuals;
        private readonly FloatingCommandBar _toolbar;
        private Rect _bounds;

        public SelectionToolbarAdorner(UIElement adornedElement, object dataContext) : base(adornedElement)
        {
            _toolbar = new FloatingCommandBar { DataContext = dataContext };
            _visuals = new VisualCollection(this) { _toolbar };
            IsHitTestVisible = true;
        }

        public void UpdateBounds(Rect rect)
        {
            _bounds = rect;
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _toolbar.Measure(constraint);
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = _toolbar.DesiredSize;
            var left = _bounds.Left + (_bounds.Width - size.Width) / 2;
            var top = _bounds.Top - size.Height - 8;
            if (top < 0) top = _bounds.Bottom + 8;
            _toolbar.Arrange(new Rect(new Point(left, top), size));
            return finalSize;
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];
    }
}
