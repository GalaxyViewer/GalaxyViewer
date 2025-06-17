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

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private UserControl _currentView;
    private readonly GridClient _client = new();
    private readonly LoginViewModel _loginViewModel;
    private readonly WelcomeViewModel _welcomeViewModel;
    private readonly LiteDbService _liteDbService;

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

    private new void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsLoggedIn => App.IsLoggedIn;

    public ICommand ExitCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NavToLoginViewCommand { get; }
    public ICommand NavToPreferencesViewCommand { get; }
    public ICommand NavToDevViewCommand { get; }

    public MainViewModel(LiteDbService liteDbService)
    {
        _liteDbService = liteDbService;

        App.StaticPropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(App.IsLoggedIn)) return;
            OnPropertyChanged(nameof(IsLoggedIn));
            if (IsLoggedIn)
            {
                Dispatcher.UIThread.Post(NavigateToLoggedInView);
            }
        };

        _loginViewModel = new LoginViewModel(_liteDbService);
        _welcomeViewModel = new WelcomeViewModel(_liteDbService);
        _currentView = new LoginView(_liteDbService);
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
        if (App.IsLoggedIn)
        {
            _client.Network.Logout();
            _loginViewModel.IsLoggedIn = false;
        }

        NavigateToLoginView();
    }

    private void NavigateToLoginView()
    {
        CurrentView = new LoginView(_liteDbService);
    }

    private void NavigateToLoggedInView()
    {
        CurrentView = new WelcomeView(_liteDbService);
    }

    private void NavigateToPreferencesView()
    {
#if ANDROID
            CurrentView = new PreferencesView { DataContext = new PreferencesViewModel() };
#else
        var preferencesWindow = new PreferencesWindow
        {
            DataContext = new PreferencesViewModel()
        };
        preferencesWindow.Show();
#endif
    }

    private void NavigateToDevView()
    {
        CurrentView = new DevView { DataContext = new DevViewModel() };
    }
}