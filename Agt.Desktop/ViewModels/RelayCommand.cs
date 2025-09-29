using System.Windows.Input;

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _exec;
    private readonly Predicate<object?>? _can;

    // existující ctor
    public RelayCommand(Action<object?> exec, Predicate<object?>? can = null)
    { _exec = exec; _can = can; }

    // NOVÝ: bezparametrový
    public RelayCommand(Action exec, Func<bool>? can = null)
    {
        _exec = _ => exec();
        _can = can is null ? null : new Predicate<object?>(_ => can());
    }

    public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _exec(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
