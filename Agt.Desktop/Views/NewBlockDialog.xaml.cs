using System;
using System.Windows;
using Agt.Desktop.Models;

namespace Agt.Desktop.Views
{
    public partial class NewBlockDialog : Window
    {
        public Block? ResultBlock { get; private set; }

        public NewBlockDialog()
        {
            InitializeComponent();
            IdBox.Text = Guid.NewGuid().ToString();
            NameBox.Text = "Nový blok";
            NameBox.Focus();
            NameBox.SelectAll();
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Zadejte název bloku.");
                return;
            }

            ResultBlock = new Block
            {
                Id = Guid.Parse(IdBox.Text),
                Name = NameBox.Text.Trim()
            };
            DialogResult = true;
        }
    }
}
