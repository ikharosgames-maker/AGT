using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Agt.Desktop.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public void PickForeground_Click(object? sender, RoutedEventArgs e)
        {
            var vm = DataContext;
            if (vm == null) return;

            var selProp = vm.GetType().GetProperty("SelectedItem");
            var item = selProp?.GetValue(vm);
            if (item == null) return;

            var fgProp = item.GetType().GetProperty("Foreground");
            var current = fgProp?.GetValue(item) as SolidColorBrush;

            var dlg = new ColorPickerWindow(current) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                fgProp?.SetValue(item, dlg.Result);
        }

        public void PickBackground_Click(object? sender, RoutedEventArgs e)
        {
            var vm = DataContext;
            if (vm == null) return;

            var selProp = vm.GetType().GetProperty("SelectedItem");
            var item = selProp?.GetValue(vm);
            if (item == null) return;

            var bgProp = item.GetType().GetProperty("Background");
            var current = bgProp?.GetValue(item) as SolidColorBrush;

            var dlg = new ColorPickerWindow(current) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                bgProp?.SetValue(item, dlg.Result);
        }
    }
}
