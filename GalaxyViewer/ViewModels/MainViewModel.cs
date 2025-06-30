using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GalaxyViewer.Views;
using GalaxyViewer.Services;
using OpenMetaverse;
using ReactiveUI;

namespace GalaxyViewer.ViewModels;

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private UserControl _currentView;
    private readonly GridClient _client;
    private readonly WelcomeViewModel _welcomeViewModel;
    private readonly LiteDbService _liteDbService;
    private readonly SessionService _sessionService;

    private PreferencesWindow? _preferencesWindow;

    public new event PropertyChangedEventHandler? PropertyChanged;

    public object CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView == value) return;
            _currentView = (UserControl)value;
            OnPropertyChanged();
        }
    }

    private new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsLoggedIn => App.IsLoggedIn;

    public ICommand ExitCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NavToLoginViewCommand { get; }
    public ICommand NavToPreferencesViewCommand { get; }
    public ICommand NavToDevViewCommand { get; }


    public MainViewModel(LiteDbService liteDbService, GridClient client,
        SessionService sessionService)
    {
        _liteDbService = liteDbService;
        _client = client;
        _sessionService = sessionService;

        App.StaticPropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(App.IsLoggedIn)) return;
            OnPropertyChanged(nameof(IsLoggedIn));
            if (IsLoggedIn)
            {
                Dispatcher.UIThread.Post(NavigateToLoggedInView);
            }
        };

        _welcomeViewModel = new WelcomeViewModel(_liteDbService, _client, _sessionService);
        _currentView = new LoginView(_liteDbService, _client);
        ExitCommand = ReactiveCommand.Create(LogoutAndExit);
        LogoutCommand = ReactiveCommand.Create(Logout);
        NavToLoginViewCommand = ReactiveCommand.Create(NavigateToLoginView);
        NavToPreferencesViewCommand = ReactiveCommand.Create(NavigateToPreferencesView);
        NavToDevViewCommand = ReactiveCommand.Create(NavigateToDevView);
    }

    private void LogoutAndExit()
    {
        Logout();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
            desktop)
        {
            desktop.Shutdown();
        }
    }

    private void Logout()
    {
        _client.Network.Logout();
        App.IsLoggedIn = false;
        NavigateToLoginView();
    }

    private void NavigateToLoginView()
    {
        CurrentView = new LoginView(_liteDbService, _client);
    }

    private void NavigateToLoggedInView()
    {
        CurrentView = new WelcomeView(_liteDbService, _client, _sessionService)
        {
            DataContext = _welcomeViewModel
        };
    }

    private void NavigateToPreferencesView()
    {
#if ANDROID
    CurrentView = new PreferencesView { DataContext = new PreferencesViewModel() };
#else
        if (_preferencesWindow is { IsVisible: false })
        {
            _preferencesWindow = new PreferencesWindow
            {
                DataContext = new PreferencesViewModel()
            };
            _preferencesWindow.Closed += (_, _) => _preferencesWindow = null;
            _preferencesWindow.Show();
        }
        else
        {
            _preferencesWindow?.Activate();
        }
#endif
    }

    private void NavigateToDevView()
    {
        CurrentView = new DevView { DataContext = new DevViewModel() };
    }
}