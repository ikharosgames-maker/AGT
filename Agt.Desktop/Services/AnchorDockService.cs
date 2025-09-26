using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows;
using Agt.Desktop.Models;

namespace Agt.Desktop.Services
{
    /// <summary>
    /// Přepočítává X/Y/Width/Height z objektu (např. FieldComponentBase) podle Dock a Anchor.
    /// Funguje nad 'object' – čte/ukládá vlastnosti reflexí:
    ///   double X,Y,Width,Height; AnchorSides Anchor; DockTo Dock
    /// </summary>
    public static class AnchorDockService
    {
        private sealed class Margins
        {
            public double Left;
            public double Top;
            public double Right;
            public double Bottom;
        }

        private static readonly ConcurrentDictionary<object, Margins> _cache = new();

        #region Reflection helpers
        private static PropertyInfo? GP(object o, string name) =>
            o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        private static bool TryGet<T>(object o, string name, out T value)
        {
            var p = GP(o, name);
            if (p != null && p.CanRead && p.PropertyType == typeof(T))
            {
                var v = p.GetValue(o);
                if (v is T t) { value = t; return true; }
            }
            value = default!;
            return false;
        }

        private static void TrySet<T>(object o, string name, T value)
        {
            var p = GP(o, name);
            if (p != null && p.CanWrite && p.PropertyType == typeof(T))
                p.SetValue(o, value);
        }
        #endregion

        public static void ResetBaseline(object item, Size parent)
        {
            if (!TryReadRect(item, out var x, out var y, out var w, out var h)) return;

            _cache[item] = new Margins
            {
                Left = x,
                Top = y,
                Right = Math.Max(0, parent.Width - (x + w)),
                Bottom = Math.Max(0, parent.Height - (y + h))
            };
        }

        public static void Apply(object item, Size parent)
        {
            if (!TryReadRect(item, out var x, out var y, out var w, out var h)) return;

            var dock = DockTo.None;
            TryGet(item, nameof(FieldComponentBase.Dock), out dock);

            if (dock != DockTo.None)
            {
                switch (dock)
                {
                    case DockTo.Left:
                        x = 0; y = 0; h = parent.Height; break;
                    case DockTo.Right:
                        x = Math.Max(0, parent.Width - w); y = 0; h = parent.Height; break;
                    case DockTo.Top:
                        x = 0; y = 0; w = parent.Width; break;
                    case DockTo.Bottom:
                        x = 0; y = Math.Max(0, parent.Height - h); w = parent.Width; break;
                    case DockTo.Fill:
                        x = 0; y = 0; w = parent.Width; h = parent.Height; break;
                }
            }
            else
            {
                var anchor = AnchorSides.Left | AnchorSides.Top;
                TryGet(item, nameof(FieldComponentBase.Anchor), out anchor);

                if (!_cache.TryGetValue(item, out var m))
                {
                    m = new Margins
                    {
                        Left = x,
                        Top = y,
                        Right = Math.Max(0, parent.Width - (x + w)),
                        Bottom = Math.Max(0, parent.Height - (y + h))
                    };
                    _cache[item] = m;
                }

                bool Has(AnchorSides s) => (anchor & s) == s;

                // horizontálně
                if (Has(AnchorSides.Left) && Has(AnchorSides.Right))
                {
                    x = m.Left;
                    w = Math.Max(0, parent.Width - m.Left - m.Right);
                }
                else if (Has(AnchorSides.Right))
                {
                    x = Math.Max(0, parent.Width - m.Right - w);
                }
                else // Left (výchozí)
                {
                    x = m.Left;
                }

                // vertikálně
                if (Has(AnchorSides.Top) && Has(AnchorSides.Bottom))
                {
                    y = m.Top;
                    h = Math.Max(0, parent.Height - m.Top - m.Bottom);
                }
                else if (Has(AnchorSides.Bottom))
                {
                    y = Math.Max(0, parent.Height - m.Bottom - h);
                }
                else // Top (výchozí)
                {
                    y = m.Top;
                }
            }

            // bounds
            x = Clamp(x, 0, Math.Max(0, parent.Width - w));
            y = Clamp(y, 0, Math.Max(0, parent.Height - h));

            TrySet(item, nameof(FieldComponentBase.X), x);
            TrySet(item, nameof(FieldComponentBase.Y), y);
            TrySet(item, nameof(FieldComponentBase.Width), w);
            TrySet(item, nameof(FieldComponentBase.Height), h);
        }

        public static void Remove(object item) => _cache.TryRemove(item, out _);

        private static bool TryReadRect(object item, out double x, out double y, out double w, out double h)
        {
            bool ok = true;
            ok &= TryGet(item, nameof(FieldComponentBase.X), out x);
            ok &= TryGet(item, nameof(FieldComponentBase.Y), out y);
            ok &= TryGet(item, nameof(FieldComponentBase.Width), out w);
            ok &= TryGet(item, nameof(FieldComponentBase.Height), out h);
            return ok;
        }

        private static double Clamp(double v, double min, double max) =>
            v < min ? min : (v > max ? max : v);
    }
}
