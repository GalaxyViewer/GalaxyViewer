using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Views;
using OpenMetaverse;
using ReactiveUI;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl? _currentView;
        private bool _isLoggedIn;
        private readonly GridClient _client = new();

        public UserControl? CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                this.RaiseAndSetIfChanged(ref _isLoggedIn, value);
                if (!value)
                {
                    NavigateToLoginView();
                }
            }
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
}