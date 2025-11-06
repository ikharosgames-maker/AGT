using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class NewProcessDialog : Window
    {
        public string EnteredName { get; private set; } = string.Empty;

        public NewProcessDialog()
        {
            InitializeComponent();
            this.Loaded += (_, __) =>
            {
                NameBox.Focus();
                NameBox.SelectAll();
            };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                ErrorText.Text = "Zadejte název procesu.";
                NameBox.Focus();
                return;
            }
            EnteredName = name;
            this.DialogResult = true;
        }
    }
}
