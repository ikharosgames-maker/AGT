using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using Agt.Desktop.ViewModels.Flow;

namespace Agt.Desktop.Converters
{
    public sealed class EdgePathConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type t, object p, CultureInfo c)
        {
            if (values.Length < 5) return Geometry.Empty;
            if (values[0] is not FlowGraphViewModel vm) return Geometry.Empty;
            if (values[1] is not Guid edgeId) return Geometry.Empty;

            var edge = vm.Edges.FirstOrDefault(e => e.Id == edgeId);
            if (edge is null) return Geometry.Empty;

            var from = vm.Nodes.FirstOrDefault(n => n.Id == edge.FromId);
            var to = vm.Nodes.FirstOrDefault(n => n.Id == edge.ToId);
            if (from is null || to is null) return Geometry.Empty;

            // uzly jsou 160x60 (viz šablona), výstup/vstup uprostřed pravého/levého okraje
            var x1 = from.X + 160; var y1 = from.Y + 30;
            var x2 = to.X; var y2 = to.Y + 30;

            var dx = Math.Max(60, Math.Abs(x2 - x1) * 0.5);
            var p1 = new System.Windows.Point(x1, y1);
            var c1 = new System.Windows.Point(x1 + dx, y1);
            var c2 = new System.Windows.Point(x2 - dx, y2);
            var p2 = new System.Windows.Point(x2, y2);

            var fig = new PathFigure { StartPoint = p1 };
            fig.Segments.Add(new BezierSegment(c1, c2, p2, true));
            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            return geo;
        }

        public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => Array.Empty<object>();
    }
}
