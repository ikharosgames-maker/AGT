using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class CloneBlockDialog : Window
    {
        public string? OriginalName
        {
            get => OriginalNameText.Text;
            set => OriginalNameText.Text = value ?? string.Empty;
        }

        public string? NewName { get; private set; }

        public CloneBlockDialog()
        {
            InitializeComponent();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            var name = NewNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nový název bloku je povinný.", "Klonovat blok",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NewName = name;
            DialogResult = true;
        }
    }
}
