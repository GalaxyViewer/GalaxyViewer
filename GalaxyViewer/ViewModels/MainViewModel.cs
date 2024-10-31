using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Views;
using OpenMetaverse;
using ReactiveUI;
using Serilog;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl? _currentView;
        private bool _isLoggedIn;

        private readonly GridClient _client = new();

        private string? _username;
        public string? Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        private string? _password;
        public string? Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private string? _loginLocation;
        public string? LoginLocation
        {
            get => _loginLocation;
            set => this.RaiseAndSetIfChanged(ref _loginLocation, value);
        }

        private string? _grid;
        public string? Grid
        {
            get => _grid;
            set => this.RaiseAndSetIfChanged(ref _grid, value);
        }

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
                CurrentView = value ? new LoggedInView() : new LoginView();
            }
        }

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ICommand ShowPreferencesCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            _currentView = new LoginView();
            IsLoggedIn = false; // By default you aren't logged in

            LogoutCommand = ReactiveCommand.Create(Logout);
            LoginCommand = ReactiveCommand.CreateFromTask(Login);
            ShowPreferencesCommand = ReactiveCommand.Create(ShowPreferences);
            ExitCommand = ReactiveCommand.Create(ExitApplication);
        }

        private void Logout()
        {
            // Perform logout operation here
            _client.Network.Logout();
            IsLoggedIn = false;
        }

        private async Task Login()
        {
            try
            {
                // Validate properties before using them
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    // Handle invalid login parameters
                    await File.AppendAllTextAsync("error.log", $"Invalid login parameters for user: {Username}");
                    return; // Exit the method if validation fails
                }

                const string userAgent = "GalaxyViewer/0.1.0";
                var loginParams = _client.Network.DefaultLoginParams(Username, Password, userAgent, LoginLocation, Grid);

                var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

                if (loginSuccess)
                {
                    IsLoggedIn = true;
                }
                else
                {
                    // Handle failed login
                    Log.Error("Failed to login user: {Username}", Username);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during login
                Log.Error(ex, "An error occurred while logging in user: {Username}", Username);
            }
        }

        private void ShowPreferences()
        {
#if ANDROID
            CurrentView = new PreferencesView();
#else
            var preferencesWindow = new PreferencesWindow
            {
                DataContext = new PreferencesViewModel()
            };
            preferencesWindow.Show();
#endif
        }

        private void ExitApplication()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
            {
                desktopLifetime.Shutdown();
            }
        }

        public void Dispose()
        {
            _client.Network.Logout();
        }
    }
}