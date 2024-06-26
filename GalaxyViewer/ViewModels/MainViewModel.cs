using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Views;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using OpenMetaverse;
using System.IO;
using System.Windows.Input;
using Serilog;
using Avalonia;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl? _currentView;
        private bool _isLoggedIn;

        private GridClient _client = new GridClient();

        // Define properties for Username, Password, LoginLocation, and Grid.
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? LoginLocation { get; set; }
        public string? Grid { get; set; }

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
                CurrentView = value ? (UserControl)new LoggedInView() : new LoginView();
            }
        }

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginWithCurrentValues { get; }

        public ICommand ShowPreferencesCommand { get; }

        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            _currentView = new LoginView();
            IsLoggedIn = false; // By default you aren't logged in

            LogoutCommand = ReactiveCommand.Create(Logout);
            LoginCommand = ReactiveCommand.Create(DisplayLoginView);
            LoginWithCurrentValues = ReactiveCommand.CreateFromTask(Login);

            ShowPreferencesCommand = ReactiveCommand.Create(ShowPreferences);

            ExitCommand = ReactiveCommand.Create(ExitApplication);
        }

        private void Logout()
        {
            // Perform logout operation here
            _client.Network.Logout();
            IsLoggedIn = false;
        }

        private void DisplayLoginView()
        {
            CurrentView = new LoginView();
        }

        public async Task Login()
        {
            try
            {
                // Validate properties before using them
                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    // Handle invalid login parameters
                    // For example, you might want to show an error message
                    // or log the invalid login attempt to a file
                    File.AppendAllText("error.log", "Invalid login parameters for user: " + Username);
                }

                string userAgent = "GalaxyViewer/0.1.0";
                LoginParams libreMetaverseLoginParams = _client.Network.DefaultLoginParams(Username, Password, userAgent, LoginLocation, Grid);

                bool loginSuccess = await Task.Run(() => _client.Network.Login(libreMetaverseLoginParams));

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
            var preferencesWindow = new PreferencesWindow
            {
                DataContext = new PreferencesViewModel()
            };
            preferencesWindow.Show();
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