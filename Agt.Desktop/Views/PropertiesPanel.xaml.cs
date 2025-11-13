using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // ====== Click handlery z XAMLu ======
        private void PickForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem("Foreground");

        private void PickBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem("Background");

        private void PickBulkForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkForeground");

        private void PickBulkBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkBackground");

        // ====== Pomůcky ======
        private void PickBrushOnSelectedItem(string propertyName)
        {
            var vm = DataContext;
            if (vm == null) return;

            var sel = vm.GetType().GetProperty("SelectedItem")?.GetValue(vm);
            if (sel == null) return;

            var prop = sel.GetType().GetProperty(propertyName);
            if (prop == null || prop.PropertyType != typeof(Brush)) return;

            var current = prop.GetValue(sel) as SolidColorBrush;
            var picked = ShowColorPicker(current);
            if (picked == null) return;

            if (prop.CanWrite)
                prop.SetValue(sel, picked);
        }

        private void PickBrushOnViewModel(string vmPropertyName)
        {
            var vm = DataContext;
            if (vm == null) return;

            var prop = vm.GetType().GetProperty(vmPropertyName);
            if (prop == null || prop.PropertyType != typeof(Brush)) return;

            var current = prop.GetValue(vm) as SolidColorBrush;
            var picked = ShowColorPicker(current);
            if (picked == null) return;

            if (prop.CanWrite)
                prop.SetValue(vm, picked);
        }

        /// <summary>
        /// Otevře ColorPickerWindow a vrátí zvolený Brush. 
        /// Podporuje ctor se SolidColorBrush? i parameterless a různé názvy výstupních vlastností.
        /// </summary>
        private Brush? ShowColorPicker(SolidColorBrush? initial)
        {
            try
            {
                // Zjisti typ okna v tomtéž assembly
                var t = typeof(PropertiesPanel).Assembly.GetType("Agt.Desktop.Views.ColorPickerWindow");
                if (t == null) return null;

                object? dlgObj = null;

                // Preferuj konstruktor se SolidColorBrush?
                var ctor = t.GetConstructors()
                            .FirstOrDefault(c =>
                            {
                                var ps = c.GetParameters();
                                return ps.Length == 1 && ps[0].ParameterType == typeof(SolidColorBrush);
                            });

                if (ctor != null)
                {
                    dlgObj = ctor.Invoke(new object?[] { initial });
                }
                else
                {
                    // fallback: parameterless
                    ctor = t.GetConstructor(Type.EmptyTypes);
                    dlgObj = ctor?.Invoke(Array.Empty<object>());
                }

                if (dlgObj is not Window dlg)
                    return null;

                dlg.Owner = Window.GetWindow(this);
                var ok = dlg.ShowDialog() == true;

                if (!ok) return null;

                // Zkusíme různé property se zvolenou hodnotou
                var props = dlgObj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                // 1) Brush/SolidColorBrush
                var brushProp = props.FirstOrDefault(pi =>
                    pi.PropertyType == typeof(Brush) || pi.PropertyType == typeof(SolidColorBrush)) ??
                    props.FirstOrDefault(pi =>
                        string.Equals(pi.Name, "ResultBrush", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pi.Name, "SelectedBrush", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pi.Name, "Brush", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pi.Name, "Result", StringComparison.OrdinalIgnoreCase));

                if (brushProp != null)
                {
                    var val = brushProp.GetValue(dlgObj) as Brush;
                    if (val != null) return val;
                }

                // 2) Color
                var colorProp = props.FirstOrDefault(pi => pi.PropertyType == typeof(Color)) ??
                                props.FirstOrDefault(pi =>
                                    string.Equals(pi.Name, "SelectedColor", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(pi.Name, "Color", StringComparison.OrdinalIgnoreCase));

                if (colorProp != null)
                {
                    var c = (Color)colorProp.GetValue(dlgObj)!;
                    return new SolidColorBrush(c);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
