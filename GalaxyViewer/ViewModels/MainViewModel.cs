using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Services;
using GalaxyViewer.Views;
using ReactiveUI;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private UserControl _currentView;
        private readonly LoginViewModel _loginViewModel;
        private readonly LoggedInViewModel _loggedInViewModel;
        private readonly SessionManager _sessionManager;

        public SessionManager SessionManager { get; }

        public MainViewModel(LoginViewModel loginViewModel, LoggedInViewModel loggedInViewModel,
            SessionManager sessionManager)
        {
            SessionManager = sessionManager;
            _loginViewModel = loginViewModel;
            _loggedInViewModel = loggedInViewModel;
            _sessionManager = sessionManager;

            // Retrieve the session from the database
            var session = _sessionManager.Session;

            // Check the session's IsLoggedIn property during initialization
            if (session != null && session.IsLoggedIn)
            {
                NavigateToLoggedInView();
            }
            else
            {
                _currentView = new LoginView { DataContext = _loginViewModel };
            }

            ExitCommand = ReactiveCommand.Create(LogoutAndExit);
            LogoutCommand = ReactiveCommand.Create(Logout);
            NavToLoginViewCommand = ReactiveCommand.Create(NavigateToLoginView);
            NavToPreferencesViewCommand = ReactiveCommand.Create(NavigateToPreferencesView);
            NavToDevViewCommand = ReactiveCommand.Create(NavigateToDevView);
        }

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

        public ICommand ExitCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand NavToLoginViewCommand { get; }
        public ICommand NavToPreferencesViewCommand { get; }
        public ICommand NavToDevViewCommand { get; }

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
            _sessionManager.Session.IsLoggedIn = false;
            NavigateToLoginView();
        }

        private void NavigateToLoginView()
        {
            CurrentView = new LoginView { DataContext = _loginViewModel };
        }

        private void NavigateToLoggedInView()
        {
            CurrentView = new LoggedInView(_loggedInViewModel);
        }

        private void NavigateToPreferencesView()
        {
            CurrentView = new PreferencesView { DataContext = new PreferencesViewModel() };
        }

        private void NavigateToDevView()
        {
            CurrentView = new DevView { DataContext = new DevViewModel() };
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public new event PropertyChangedEventHandler PropertyChanged;
    }
}