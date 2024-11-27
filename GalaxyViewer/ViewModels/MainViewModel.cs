using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Views;
using OpenMetaverse;
using ReactiveUI;

namespace GalaxyViewer.ViewModels;

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private UserControl _currentView;
    private bool _isLoggedIn;
    private readonly GridClient _client = new();

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

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set { this.RaiseAndSetIfChanged(ref _isLoggedIn, value); }
    }

    public ICommand ExitCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand NavToLoginViewCommand { get; }
    public ICommand NavToPreferencesViewCommand { get; }

    public MainViewModel()
    {
        _currentView = new LoginView { DataContext = new LoginViewModel() };
        ExitCommand = ReactiveCommand.Create(LogoutAndExit);
        LogoutCommand = ReactiveCommand.Create(Logout);
        NavToLoginViewCommand = ReactiveCommand.Create(NavigateToLoginView);
        NavToPreferencesViewCommand = ReactiveCommand.Create(NavigateToPreferencesView);
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
        if (IsLoggedIn)
        {
            _client.Network.Logout();
            IsLoggedIn = false;
        }

        NavigateToLoginView();
    }

    private void NavigateToLoginView()
    {
        CurrentView = new LoginView { DataContext = new LoginViewModel() };
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
}