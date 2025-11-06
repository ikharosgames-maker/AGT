using System;
using System.Windows;
using System.Windows.Input;

namespace Agt.Desktop.Views
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();

            var vm = new ViewModels.CasesDashboardViewModel();

            vm.OpenEditorRequested += (_, __) =>
            {
                var wnd = new FormProcessEditorWindow { Owner = this };
                wnd.ShowDialog();
            };

            vm.StartNewCaseRequested += (_, __) =>
            {
                var dlg = new StartCaseDialog { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    var row = new ViewModels.CaseRow
                    {
                        FormName      = $"Formulář {DateTime.Now:yyyy-MM-dd HH:mm}",
                        CreatedAt     = DateTime.Now,
                        CreatedBy     = Environment.UserName,
                        StageProgress = "1/1",
                        Assignees     = Environment.UserName,
                        DueAt         = null,
                        CurrentStage  = "Stage 1"
                    };
                    vm.Items.Insert(0, row);
                    vm.Selected = row;
                }
            };

            vm.OpenCaseRequested += (_, row) =>
            {
                if (row == null) return;
                MessageBox.Show($"Otevírám case: {row.FormName}", "Spustit case",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            };

            DataContext = vm;
        }

        private void CasesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.CasesDashboardViewModel vm
                && vm.OpenCaseCommand.CanExecute(null))
            {
                vm.OpenCaseCommand.Execute(null);
            }
        }
    }
}
