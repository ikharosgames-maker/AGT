using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Agt.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel pro přehled case na hlavním okně (dashboard).
    /// Sloupce: FormName, CreatedAt, CreatedBy, StageProgress, Assignees, DueAt, CurrentStage.
    /// Vyhlašuje události, které obslouží MainShell.xaml.cs (otevírání oken, založení case).
    /// </summary>
    public sealed class CasesDashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== Události zachytí MainShell.xaml.cs =====
        public event EventHandler? StartNewCaseRequested;
        public event EventHandler? OpenEditorRequested;
        public event EventHandler<CaseRow?>? OpenCaseRequested;

        // Data pro mřížku
        public ObservableCollection<CaseRow> Items { get; } = new();
        public ObservableCollection<CaseRow> Cases => Items; // alias, pokud je v XAMLu

        private CaseRow? _selected;
        public CaseRow? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedCase)); }
        }

        public CaseRow? SelectedCase
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected)); }
        }

        // Příkazy pro MainShell.xaml
        public ICommand NewCaseCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand OpenEditorCommand { get; }
        public ICommand OpenCaseCommand { get; }
        public ICommand RefreshCommand { get; }

        public CasesDashboardViewModel()
        {
            NewCaseCommand = new UiCommand(_ => StartNewCaseRequested?.Invoke(this, EventArgs.Empty));
            ExitCommand = new UiCommand(_ => Agt.Desktop.App.Current?.Shutdown());
            OpenEditorCommand = new UiCommand(_ => OpenEditorRequested?.Invoke(this, EventArgs.Empty));
            OpenCaseCommand = new UiCommand(_ => OpenCaseRequested?.Invoke(this, SelectedCase), _ => SelectedCase != null);
            RefreshCommand = new UiCommand(_ => Refresh());

            // Demo: ať je UI živé (nahradí se reálnými daty)
            if (Items.Count == 0)
            {
                Items.Add(new CaseRow
                {
                    FormName = "Ukázkový formulář",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    CreatedBy = Environment.UserName,
                    StageProgress = "1/3",
                    Assignees = Environment.UserName,
                    DueAt = DateTime.Now.AddDays(3),
                    CurrentStage = "Stage 1"
                });
            }
        }

        private void Refresh()
        {
            // TODO: načíst reálná data z úložiště
        }

        // Jednoduchá ICommand implementace
        private sealed class UiCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public UiCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Řádek v přehledu – jména property odpovídají bindingům v MainShell.xaml.
    /// </summary>
    public sealed class CaseRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _formName = string.Empty;
        public string FormName { get => _formName; set { _formName = value; OnPropertyChanged(); } }

        private DateTime _createdAt = DateTime.Now;
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; OnPropertyChanged(); } }

        private string _createdBy = string.Empty;
        public string CreatedBy { get => _createdBy; set { _createdBy = value; OnPropertyChanged(); } }

        private string _stageProgress = "0/0";
        public string StageProgress { get => _stageProgress; set { _stageProgress = value; OnPropertyChanged(); } }

        private string _assignees = string.Empty;
        public string Assignees { get => _assignees; set { _assignees = value; OnPropertyChanged(); } }

        private DateTime? _dueAt = null;
        public DateTime? DueAt { get => _dueAt; set { _dueAt = value; OnPropertyChanged(); } }

        private string _currentStage = string.Empty;
        public string CurrentStage { get => _currentStage; set { _currentStage = value; OnPropertyChanged(); } }
    }
}
