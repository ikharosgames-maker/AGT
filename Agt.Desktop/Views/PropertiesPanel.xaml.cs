using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public partial class PropertiesPanel : UserControl
    {
        public PropertiesPanel()
        {
            InitializeComponent();
        }

        private void RemoveOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.CommandParameter is not string value) return;
            if (btn.DataContext is ComboBoxField cb)
                cb.Options.Remove(value);
        }

        private void AddOption_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            var text = tb.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (tb.Tag is ComboBoxField cb)
                cb.Options.Add(text);
            tb.Clear();
        }
    }
}
