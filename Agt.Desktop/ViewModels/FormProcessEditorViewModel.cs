using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Agt.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel editoru formulářů.
    /// Přidává „Založit nový“ (NewFormCommand) a bezpečně uzavírá třídu/namespace.
    /// </summary>
    public sealed class FormProcessEditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _formKey = string.Empty;
        public string FormKey
        {
            get => _formKey;
            set { _formKey = value; OnPropertyChanged(); }
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public ICommand NewFormCommand { get; }

        public FormProcessEditorViewModel()
        {
            NewFormCommand = new RelayCommand(_ => CreateNewForm());
        }

        /// <summary>
        /// Vytvoří nový prázdný formulář s jedinečným Id a názvem.
        /// </summary>
        private void CreateNewForm()
        {
            var now = DateTime.Now;
            FormKey = $"form-{now:yyyyMMdd-HHmmss}";
            Title = $"Nový formulář {now:yyyy-MM-dd HH:mm}";

            // TODO: vyčistit plátno, přidat výchozí Stage 1 atd., dle vaší implementace editoru.
        }
    }

    /// <summary>
    /// Jednoduchý RelayCommand pro ICommand.
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}