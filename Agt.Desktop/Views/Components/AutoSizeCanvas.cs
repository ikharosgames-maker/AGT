using System.Windows;
using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Canvas, který si spočítá DesiredSize podle dětí (minX..maxX, minY..maxY) + Padding
    /// a volitelně normalizuje souřadnice tak, aby minX/minY začínaly u levého-horního rohu (tj. u Padding).
    /// </summary>
    public class AutoSizeCanvas : Canvas
    {
        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(
                nameof(Padding),
                typeof(Thickness),
                typeof(AutoSizeCanvas),
                new FrameworkPropertyMetadata(new Thickness(12), FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Pokud true (default), plátno vizuálně posune děti tak, aby minX/minY ležely na Padding.Left/Top.</summary>
        public static readonly DependencyProperty NormalizeToTopLeftProperty =
            DependencyProperty.Register(
                nameof(NormalizeToTopLeft),
                typeof(bool),
                typeof(AutoSizeCanvas),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Thickness Padding
        {
            get => (Thickness)GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public bool NormalizeToTopLeft
        {
            get => (bool)GetValue(NormalizeToTopLeftProperty);
            set => SetValue(NormalizeToTopLeftProperty, value);
        }

        private double _minX, _minY, _maxX, _maxY;

        protected override Size MeasureOverride(Size constraint)
        {
            _minX = double.PositiveInfinity;
            _minY = double.PositiveInfinity;
            _maxX = 0;
            _maxY = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;

                child.Measure(constraint);

                double x = GetLeft(child);
                double y = GetTop(child);
                if (double.IsNaN(x)) x = 0;
                if (double.IsNaN(y)) y = 0;

                _minX = System.Math.Min(_minX, x);
                _minY = System.Math.Min(_minY, y);
                _maxX = System.Math.Max(_maxX, x + child.DesiredSize.Width);
                _maxY = System.Math.Max(_maxY, y + child.DesiredSize.Height);
            }

            if (double.IsPositiveInfinity(_minX)) { _minX = 0; _minY = 0; } // žádné děti

            double width = System.Math.Max(0, (_maxX - _minX)) + Padding.Left + Padding.Right;
            double height = System.Math.Max(0, (_maxY - _minY)) + Padding.Top + Padding.Bottom;

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // o kolik vizuálně posuneme děti (tak, aby minX/minY ležely na levém/horním paddingu)
            double offsetX = NormalizeToTopLeft ? (_minX - Padding.Left) : 0;
            double offsetY = NormalizeToTopLeft ? (_minY - Padding.Top) : 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null) continue;

                double x = GetLeft(child);
                double y = GetTop(child);
                if (double.IsNaN(x)) x = 0;
                if (double.IsNaN(y)) y = 0;

                // nová pozice = původní – offset + padding
                double nx = x - offsetX;
                double ny = y - offsetY;

                child.Arrange(new Rect(new Point(nx, ny), child.DesiredSize));
            }

            return arrangeSize;
        }
    }
}
