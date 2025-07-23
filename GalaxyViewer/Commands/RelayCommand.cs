using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GalaxyViewer.Commands;

public class RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    : ICommand
{
    private readonly Func<Task> _execute =
        execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter) => await _execute();

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    private abstract class CommandManager
    {
        public static EventHandler? RequerySuggested { get; set; }
    }
}