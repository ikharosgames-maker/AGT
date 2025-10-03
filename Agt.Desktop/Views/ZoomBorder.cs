using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public class ZoomBorder : Border
    {
        private UIElement? _child;
        private Point _origin, _start;
        private readonly ScaleTransform _scale = new();
        private readonly TranslateTransform _translate = new();

        public override UIElement? Child
        {
            get => base.Child;
            set
            {
                base.Child = value;
                _child = value;
                if (_child is null) return;

                var group = new TransformGroup();
                group.Children.Add(_scale);
                group.Children.Add(_translate);
                _child.RenderTransform = group;
                _child.RenderTransformOrigin = new Point(0, 0);

                MouseWheel += OnMouseWheel;
                MouseLeftButtonDown += OnMouseLeftButtonDown;
                MouseLeftButtonUp += (s, e) => { Cursor = Cursors.Arrow; _child.ReleaseMouseCapture(); };
                MouseMove += OnMouseMove;
                PreviewMouseRightButtonDown += (s, e) => Reset();
            }
        }

        public void Reset()
        {
            _scale.ScaleX = _scale.ScaleY = 1.0;
            _translate.X = _translate.Y = 0.0;
        }

        private void OnMouseLeftButtonDown(object s, MouseButtonEventArgs e)
        {
            if (_child is null) return;
            Cursor = Cursors.Hand;
            _start = e.GetPosition(this);
            _origin = new Point(_translate.X, _translate.Y);
            _child.CaptureMouse();
        }

        private void OnMouseMove(object s, MouseEventArgs e)
        {
            if (_child is null || !_child.IsMouseCaptured) return;
            var v = _start - e.GetPosition(this);
            _translate.X = _origin.X - v.X;
            _translate.Y = _origin.Y - v.Y;
        }

        private void OnMouseWheel(object s, MouseWheelEventArgs e)
        {
            if (_child is null) return;
            var pos = e.GetPosition(_child);
            var zoom = e.Delta > 0 ? .1 : -.1;
            if ((_scale.ScaleX + zoom) < 0.2) return;

            _scale.ScaleX += zoom;
            _scale.ScaleY += zoom;

            _translate.X = -1 * (pos.X * _scale.ScaleX - e.GetPosition(this).X);
            _translate.Y = -1 * (pos.Y * _scale.ScaleY - e.GetPosition(this).Y);
        }
    }
}
