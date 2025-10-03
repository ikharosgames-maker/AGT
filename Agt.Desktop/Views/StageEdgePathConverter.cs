using Agt.Desktop.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public sealed class StageEdgePathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Geometry.Empty;
            if (values[0] is not FormProcessEditorViewModel root) return Geometry.Empty;
            if (values[1] is not Guid edgeId) return Geometry.Empty;

            var edge = root.Graph.StageEdges.FirstOrDefault(e => e.Id == edgeId);
            if (edge is null) return Geometry.Empty;

            var from = root.FindStage(edge.FromStageId);
            var to = root.FindStage(edge.ToStageId);
            if (from is null || to is null) return Geometry.Empty;

            var s1 = root.GetStageOutPortAbs(from); // (X,Y)
            var s2 = root.GetStageInPortAbs(to);

            var p1 = new Point(s1.X, s1.Y);
            var p2 = new Point(s2.X, s2.Y);

            var dx = Math.Max(60, Math.Abs(p2.X - p1.X) * 0.5);
            var c1 = new Point(p1.X + dx, p1.Y);
            var c2 = new Point(p2.X - dx, p2.Y);

            var fig = new PathFigure { StartPoint = p1 };
            fig.Segments.Add(new BezierSegment(c1, c2, p2, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            return geo;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => Array.Empty<object>();
    }
}
