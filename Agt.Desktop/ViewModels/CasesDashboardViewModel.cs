using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Agt.Domain.Abstractions;
using Agt.Domain.Models;
using Agt.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Agt.Desktop.ViewModels
{
    public sealed class CasesDashboardViewModel : ViewModelBase
    {
        public ObservableCollection<CaseRow> Cases { get; } = new();

        public string? FilterText { get; set; }
        public string[] StateOptions { get; } = new[] { "Vše", "Otevřené", "Uzavřené", "Čekající" };
        public string SelectedState { get; set; } = "Vše";
        public bool CanAdmin { get; set; } = true; // TODO: napojit na IAuthZ

        public ICommand RefreshCommand { get; }
        public ICommand NewCaseCommand { get; }
        public ICommand OpenCaseCommand { get; }
        public ICommand CompleteSelectedBlockCommand { get; }
        public ICommand ReopenSelectedBlockCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand OpenBlockEditorCommand { get; }
        public ICommand OpenFormEditorCommand { get; }
        public ICommand OpenRoutingEditorCommand { get; }
        public ICommand OpenMyTasksCommand { get; }
        public ICommand OpenNotificationsCommand { get; }
        public ICommand OpenUsersRolesCommand { get; }
        public ICommand OpenPublishCenterCommand { get; }

        private readonly ICaseRepository _casesRepo;
        private readonly ITaskRepository _tasksRepo;
        private readonly ICaseService _caseService;

        public CasesDashboardViewModel()
        {
            // 🔑 Jediný správný vstup do DI v desktopu:
            var sp = Agt.Desktop.App.Services;

            _casesRepo = sp.GetRequiredService<ICaseRepository>();
            _tasksRepo = sp.GetRequiredService<ITaskRepository>();
            _caseService = sp.GetRequiredService<ICaseService>();

            RefreshCommand = new RelayCommand(Load);
            NewCaseCommand = new RelayCommand(StartNewCase);
            OpenCaseCommand = new RelayCommand(OpenSelected);
            CompleteSelectedBlockCommand = new RelayCommand(CompleteSelectedBlock);
            ReopenSelectedBlockCommand = new RelayCommand(ReopenSelectedBlock);
            ExitCommand = new RelayCommand(() => Agt.Desktop.App.Current.Shutdown());

            OpenBlockEditorCommand = new RelayCommand(() => {
                var win = new Agt.Desktop.Views.BlocksEditorWindow();
                win.Owner = Agt.Desktop.App.Current.MainWindow;
                win.Show();
            });
            OpenFormEditorCommand = new RelayCommand(() =>
            {
                var win = new Agt.Desktop.Views.FormProcessEditorWindow();
                win.Owner = Agt.Desktop.App.Current.MainWindow;
                win.Show();
            });
            OpenRoutingEditorCommand = new RelayCommand(() => MessageBox.Show("Routing editor – WIP"));
            OpenMyTasksCommand = new RelayCommand(() => MessageBox.Show("Moje úkoly – WIP"));
            OpenNotificationsCommand = new RelayCommand(() => MessageBox.Show("Notifikace – WIP"));
            OpenUsersRolesCommand = new RelayCommand(() => MessageBox.Show("Uživatelé a role – WIP"));
            OpenPublishCenterCommand = new RelayCommand(() => MessageBox.Show("Publikace verzí – WIP"));

            Load();
        }

        private void Load()
        {
            Cases.Clear();
            var data = _casesRepo.ListRecent(500).ToList();

            foreach (var c in data)
            {
                var blocks = _casesRepo.ListBlocks(c.Id).ToList();
                var openBlocks = blocks.Where(b => b.State is CaseBlockState.Open).ToList();

                Cases.Add(new CaseRow
                {
                    Id = c.Id,
                    FormVersion = c.FormVersionId.ToString().Substring(0, 8),
                    StartedBy = c.StartedBy.ToString().Substring(0, 8),
                    StartedAt = c.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    OpenBlocks = openBlocks.Count,
                    Assignees = string.Join(",",
                        openBlocks.Select(b => b.AssigneeUserId is null ? "—" : b.AssigneeUserId.Value.ToString().Substring(0, 8))),
                    DueAt = openBlocks.Min(b => b.DueAt)?.ToString("yyyy-MM-dd") ?? "",
                    Status = openBlocks.Any() ? "Otevřený" : "V procesu/Locked"
                });
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var ft = FilterText.Trim().ToLowerInvariant();
                var filtered = Cases.Where(r =>
                    r.Id.ToString().ToLower().Contains(ft) ||
                    r.FormVersion.ToLower().Contains(ft) ||
                    r.StartedBy.ToLower().Contains(ft)).ToList();

                Cases.Clear();
                foreach (var r in filtered) Cases.Add(r);
            }
        }

        private void StartNewCase()
        {
            MessageBox.Show("TODO: dialog pro výběr FormVersion + startovní bloky → ICaseService.StartCase(...).");
            Load();
        }

        private void OpenSelected()
        {
            MessageBox.Show("TODO: otevřít detail vybraného Case (nové okno).");
        }

        private void CompleteSelectedBlock()
        {
            MessageBox.Show("TODO: vybrat blok a zavolat ICaseService.CompleteBlock(...).");
        }

        private void ReopenSelectedBlock()
        {
            MessageBox.Show("TODO: vybrat blok a ICaseService.ReopenBlock(...).");
        }
    }

    public sealed class CaseRow
    {
        public Guid Id { get; set; }
        public string FormVersion { get; set; } = "";
        public string StartedBy { get; set; } = "";
        public string StartedAt { get; set; } = "";
        public int OpenBlocks { get; set; }
        public string Assignees { get; set; } = "";
        public string DueAt { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
