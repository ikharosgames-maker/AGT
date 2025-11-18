using System;
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

        // ========= CLICK HANDLERY – 1 vybraná položka =========

        private void PickForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem(nameof(Models.FieldComponentBase.Foreground));

        private void PickBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem(nameof(Models.FieldComponentBase.Background));

        private void PickLabelForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem(nameof(Models.FieldComponentBase.LabelForeground));

        private void PickLabelBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnSelectedItem(nameof(Models.FieldComponentBase.LabelBackground));

        // ========= CLICK HANDLERY – HROMADNÁ ÚPRAVA =========

        private void PickBulkForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkForeground");

        private void PickBulkBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkBackground");

        private void PickBulkLabelForeground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkLabelForeground");

        private void PickBulkLabelBackground_Click(object sender, RoutedEventArgs e)
            => PickBrushOnViewModel("BulkLabelBackground");

        // ========= POMOCNÉ METODY =========

        /// <summary>
        /// Vybere barvu pro aktuálně vybranou komponentu (SelectedItem.FieldComponentBase.*).
        /// </summary>
        private void PickBrushOnSelectedItem(string propertyName)
        {
            if (DataContext is null)
                return;

            var vmType = DataContext.GetType();
            var selectedProp = vmType.GetProperty("SelectedItem");
            if (selectedProp == null)
                return;

            var selectedItem = selectedProp.GetValue(DataContext);
            if (selectedItem is null)
                return;

            var targetProp = selectedItem.GetType().GetProperty(propertyName);
            if (targetProp == null || !typeof(Brush).IsAssignableFrom(targetProp.PropertyType))
                return;

            var currentBrush = targetProp.GetValue(selectedItem) as SolidColorBrush;
            var picked = ShowColorPicker(currentBrush);
            if (picked == null)
                return;

            if (targetProp.CanWrite)
                targetProp.SetValue(selectedItem, picked);
        }

        /// <summary>
        /// Vybere barvu pro hromadné nastavení (Bulk* property na DesignerViewModelu).
        /// </summary>
        private void PickBrushOnViewModel(string propertyName)
        {
            if (DataContext is null)
                return;

            var vmType = DataContext.GetType();
            var targetProp = vmType.GetProperty(propertyName);
            if (targetProp == null || !typeof(Brush).IsAssignableFrom(targetProp.PropertyType))
                return;

            var currentBrush = targetProp.GetValue(DataContext) as SolidColorBrush;
            var picked = ShowColorPicker(currentBrush);
            if (picked == null)
                return;

            if (targetProp.CanWrite)
                targetProp.SetValue(DataContext, picked);
        }

        /// <summary>
        /// Otevře naše WPF ColorPickerWindow a vrátí vybranou barvu.
        /// </summary>
        private SolidColorBrush? ShowColorPicker(SolidColorBrush? initial)
        {
            try
            {
                var dlg = new ColorPickerWindow(initial);
                var owner = Window.GetWindow(this);
                if (owner != null)
                    dlg.Owner = owner;

                var result = dlg.ShowDialog();
                if (result == true)
                    return dlg.ResultBrush;

                return null;
            }
            catch
            {
                // nechceme shodit UI kvůli pickeru
                return null;
            }
        }
    }
}
