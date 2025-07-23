using System;
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

public class MainViewModel : ViewModelBase, INotifyPropertyChanged, IDisposable
{
    private UserControl _currentView;
    private readonly GridClient _client;
    private readonly LiteDbService _liteDbService;
    private readonly SessionService _sessionService;
    private readonly ChatService _chatService;
    private readonly DashboardView _dashboardView;
    private bool _disposed;

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
    public ICommand BackToDashboardViewCommand { get; }


    public MainViewModel(LiteDbService liteDbService, GridClient client,
        SessionService sessionService)
    {
        _liteDbService = liteDbService;
        _client = client;
        _sessionService = sessionService;
        _chatService = new ChatService(_client, _liteDbService);

        ExitCommand = ReactiveCommand.Create(LogoutAndExit);
        LogoutCommand = ReactiveCommand.Create(Logout);
        NavToLoginViewCommand = ReactiveCommand.Create(NavigateToLoginView);
        NavToPreferencesViewCommand = ReactiveCommand.Create(NavigateToPreferencesView);
        NavToDevViewCommand = ReactiveCommand.Create(NavigateToDevView);
        BackToDashboardViewCommand = ReactiveCommand.Create(NavigateBackToDashboardView);

        var dashboardViewModel = new DashboardViewModel(_liteDbService, _client, _sessionService,
            NavToPreferencesViewCommand, _chatService);
        _dashboardView = new DashboardView { DataContext = dashboardViewModel };

        App.StaticPropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(App.IsLoggedIn)) return;
            OnPropertyChanged(nameof(IsLoggedIn));
            if (IsLoggedIn)
            {
                Dispatcher.UIThread.Post(NavigateToDashboardView);
            }
        };

        var loginViewModel = new LoginViewModel(_liteDbService, _client);
        _currentView = new LoginView { DataContext = loginViewModel };
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
        _chatService.Dispose();
        _client.Network.Logout();
        App.IsLoggedIn = false;
        NavigateToLoginView();
    }

    private void NavigateToLoginView()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var loginViewModel = new LoginViewModel(_liteDbService, _client);
            var loginView = new LoginView { DataContext = loginViewModel };
            CurrentView = loginView;

            if (OperatingSystem.IsAndroid())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    loginView.Focus();
                }, DispatcherPriority.Loaded);
            }
        });
    }

    private void NavigateToDashboardView()
    {
        CurrentView = _dashboardView;
    }

    private void NavigateToPreferencesView()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            CurrentView = new PreferencesView { DataContext = new PreferencesViewModel(BackToDashboardViewCommand) };
        }
        else
        {
            if (_preferencesWindow is not { IsVisible: true })
            {
                _preferencesWindow?.Close();
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
        }
    }

    private void NavigateBackToDashboardView()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (App.IsLoggedIn)
            {
                NavigateToDashboardView();
            }
            else
            {
                NavigateToLoginView();
            }
        });
    }

    private void NavigateToDevView()
    {
        CurrentView = new DevView { DataContext = new DevViewModel() };
    }


    private readonly object _disposeLock = new();
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _chatService.Dispose();
        }
        _disposed = true;
    }

    ~MainViewModel()
    {
        Dispose(false);
    }
}