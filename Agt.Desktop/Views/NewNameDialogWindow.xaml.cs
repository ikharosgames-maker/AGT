using System;
using System.Windows;

namespace Agt.Desktop.Views
{
    /// <summary>
    /// Univerzální dialog pro zadání názvu. GUID se generuje na pozadí.
    /// </summary>
    public partial class NewNameDialogWindow : Window
    {
        /// <summary>Typ objektu pro zobrazení v titulku, např. "formulář", "blok".</summary>
        public string ObjectType { get; }

        /// <summary>Výsledný název z textboxu.</summary>
        public string NameValue { get; private set; } = string.Empty;

        /// <summary>Vygenerovaný GUID pro novou entitu.</summary>
        public Guid IdValue { get; private set; }

        public NewNameDialogWindow(string objectType = "objekt", string? defaultName = null)
        {
            InitializeComponent();

            ObjectType = string.IsNullOrWhiteSpace(objectType) ? "objekt" : objectType.Trim();

            Title = $"Nový {ObjectType}";
            TbCaption.Text = $"Nový {ObjectType}";

            TbName.Text = defaultName ?? string.Empty;

            // GUID jen v paměti, nezobrazuje se
            IdValue = Guid.NewGuid();
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            NameValue = TbName.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(NameValue))
            {
                MessageBox.Show("Zadejte název.", "Nový objekt", MessageBoxButton.OK, MessageBoxImage.Warning);
                TbName.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
