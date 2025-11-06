using System;
using System.Windows;

namespace Agt.Desktop.Views
{
    public partial class MainShell : Window
    {
        public MainShell()
        {
            InitializeComponent();

            var vm = new ViewModels.CasesDashboardViewModel();

            // otevřít editor
            vm.OpenEditorRequested += (_, __) =>
            {
                var wnd = new FormProcessEditorWindow { Owner = this };
                wnd.ShowDialog();
            };

            // založit nový case → otevřít StartCaseDialog a po OK přidat do přehledu
            vm.StartNewCaseRequested += (_, __) =>
            {
                var dlg = new StartCaseDialog { Owner = this };
                if (dlg.ShowDialog() == true)
                {
                    // v tuto chvíli jen vytvoříme „živý“ záznam do dashboardu
                    var row = new ViewModels.CaseRow
                    {
                        FormName = $"Formulář {DateTime.Now:yyyy-MM-dd HH:mm}",
                        CreatedAt = DateTime.Now,
                        CreatedBy = Environment.UserName,
                        StageProgress = "1/1",
                        Assignees = Environment.UserName,
                        DueAt = null,
                        CurrentStage = "Stage 1"
                    };
                    vm.Items.Insert(0, row);
                    vm.Selected = row;

                    // TODO: napojit na ICaseDataRepository (uložit snapshot) + rovnou otevřít Case Run okno
                }
            };

            // otevřít existující case (zatím jen info)
            vm.OpenCaseRequested += (_, row) =>
            {
                if (row == null) return;
                MessageBox.Show($"Otevřít case: {row.FormName}", "Otevřít case",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: tady otevřeme Case Run okno s rozložením bloků podle stagi
            };

            DataContext = vm;
        }
        private void CasesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.CasesDashboardViewModel vm && vm.OpenCaseCommand.CanExecute(null))
            {
                vm.OpenCaseCommand.Execute(null);
            }
        }
    }
}
