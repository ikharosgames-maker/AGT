using System;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class PublishFormDialog : Window
    {
        public string FormName => NameBox.Text.Trim();
        public string FormKey => KeyBox.Text.Trim();
        public string Version => VersionBox.Text.Trim();

        public PublishFormDialog()
        {
            InitializeComponent();
            NameBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FormName) || string.IsNullOrWhiteSpace(FormKey) || string.IsNullOrWhiteSpace(Version))
            {
                MessageBox.Show("Vyplň název, key i verzi.", "Publikovat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }
    }
}
