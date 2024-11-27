﻿using System;
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
    private readonly GridClient _client = new();
    private readonly LoginViewModel _loginViewModel;

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

    public MainViewModel()
    {
        App.StaticPropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(App.IsLoggedIn))
            {
                OnPropertyChanged(nameof(IsLoggedIn));
            }
        };

        _loginViewModel = new LoginViewModel();
        _currentView = new LoginView { DataContext = _loginViewModel };
        ExitCommand = ReactiveCommand.Create(LogoutAndExit);
        LogoutCommand = ReactiveCommand.Create(Logout);
        NavToLoginViewCommand = ReactiveCommand.Create(NavigateToLoginView);
        NavToPreferencesViewCommand = ReactiveCommand.Create(NavigateToPreferencesView);
    }

    private void LogoutAndExit()
    {
        Logout();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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
        CurrentView = new LoginView { DataContext = _loginViewModel };
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