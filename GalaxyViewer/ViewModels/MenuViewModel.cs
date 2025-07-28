using System.Windows.Input;

namespace GalaxyViewer.ViewModels;

public class MenuViewModel : ViewModelBase
{
    public ICommand NavToLoginViewCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NavToPreferencesViewCommand { get; }
    public ICommand NavToDevViewCommand { get; }
    public ICommand BackToDashboardViewCommand { get; }
    public ICommand ExitCommand { get; }
    // Add more shared commands as needed

    public MenuViewModel(
        ICommand navToLoginViewCommand,
        ICommand logoutCommand,
        ICommand navToPreferencesViewCommand,
        ICommand navToDevViewCommand,
        ICommand backToDashboardViewCommand,
        ICommand exitCommand)
    {
        NavToLoginViewCommand = navToLoginViewCommand;
        LogoutCommand = logoutCommand;
        NavToPreferencesViewCommand = navToPreferencesViewCommand;
        NavToDevViewCommand = navToDevViewCommand;
        BackToDashboardViewCommand = backToDashboardViewCommand;
        ExitCommand = exitCommand;
    }
}

