using System.Windows.Controls;

namespace Agt.Desktop.Views
{
    public partial class FormEditorView : UserControl
    {
        public FormEditorView()
        {
            InitializeComponent();
            DataContext = new ViewModels.FormEditorViewModel();
        }
    }
}
