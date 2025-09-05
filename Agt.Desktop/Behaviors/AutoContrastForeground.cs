using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Agt.Desktop.Behaviors
{
    public static class AutoContrastForeground
    {
        public static readonly DependencyProperty MonitorBackgroundProperty =
            DependencyProperty.RegisterAttached("MonitorBackground", typeof(bool), typeof(AutoContrastForeground),
                new PropertyMetadata(false, OnMonitorChanged));

        public static void SetMonitorBackground(DependencyObject element, bool value) =>
            element.SetValue(MonitorBackgroundProperty, value);
        public static bool GetMonitorBackground(DependencyObject element) =>
            (bool)element.GetValue(MonitorBackgroundProperty);

        private static void OnMonitorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Control c) return;
            if ((bool)e.NewValue)
            {
                c.Loaded += (_, __) => Update(c);
                DependencyPropertyDescriptor.FromProperty(Control.BackgroundProperty, typeof(Control))
                    .AddValueChanged(c, (_, __) => Update(c));
                Update(c);
            }
        }

        private static void Update(Control c)
        {
            var bg = (c.Background as SolidColorBrush)?.Color ?? Colors.Transparent;
            if (bg == Colors.Transparent)
            {
                var brush = FindEffectiveBackground(c);
                if (brush is SolidColorBrush sb) bg = sb.Color;
            }
            var lum = RelativeLuminance(bg);
            c.Foreground = lum > 0.5 ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White);
        }

        private static Brush? FindEffectiveBackground(DependencyObject start)
        {
            var cur = start;
            while (cur != null)
            {
                if (cur is Panel p && p.Background != null) return p.Background;
                if (cur is Control c && c.Background != null) return c.Background;
                cur = VisualTreeHelper.GetParent(cur);
            }
            return null;
        }

        private static double RelativeLuminance(Color c)
        {
            static double Lin(double x) => (x <= 0.04045) ? x / 12.92 : System.Math.Pow((x + 0.055) / 1.055, 2.4);
            var r = Lin(c.R / 255.0); var g = Lin(c.G / 255.0); var b = Lin(c.B / 255.0);
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}
