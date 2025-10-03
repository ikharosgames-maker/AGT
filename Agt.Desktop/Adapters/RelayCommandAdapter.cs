using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Agt.Desktop.Adapters
{
    public sealed class RelayCommandAdapter : IRelayCommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommandAdapter(Action execute, Func<bool>? canExecute = null)
        {
            _execute = _ => execute();
            _canExecute = canExecute is null ? null : new Func<object?, bool>(_ => canExecute());
        }

        public RelayCommandAdapter(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void NotifyCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        // explicitní implementace ICommand (IRelayCommand dědí z ICommand)
        bool ICommand.CanExecute(object? parameter) => CanExecute(parameter);
        void ICommand.Execute(object? parameter) => Execute(parameter);
    }
}
