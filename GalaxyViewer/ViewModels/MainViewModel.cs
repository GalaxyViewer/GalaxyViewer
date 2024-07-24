using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Services;
using GalaxyViewer.Views;
using OpenMetaverse;
using ReactiveUI;
using Serilog;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
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

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => this.RaiseAndSetIfChanged(ref _isLoggedIn, value);
        }

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }

        public ICommand ShowPreferencesCommand { get; }
        public ICommand ShowDevViewCommand { get; }

        public ICommand ExitCommand { get; }

        public MainViewModel(NavigationService navigationService)
        {
            IsLoggedIn = false; // By default you aren't logged in

            LogoutCommand = ReactiveCommand.Create(Logout);
            LoginCommand = ReactiveCommand.Create(() => navigationService.Navigate("login"));

            ShowPreferencesCommand = ReactiveCommand.Create(ShowPreferences);
            ShowDevViewCommand = ReactiveCommand.Create(() => navigationService.Navigate("debug"));

            ExitCommand = ReactiveCommand.Create(ExitApplication);

            // Navigate to login view on startup
            if (!IsLoggedIn)
            {
                navigationService.Navigate("login");
            }
        }

        private void Logout()
        {
            // Perform logout operation here
            _client.Network.Logout();
            IsLoggedIn = false;
        }

        public async Task Login(string username, string password)
        {
            /*try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    await File.AppendAllTextAsync("error.log",
                        "Invalid login parameters for user: " + username);
                    return;
                }

                const string userAgent = "GalaxyViewer/0.1.0";
                var libreMetaverseLoginParams =
                    _client.Network.DefaultLoginParams(username, password, userAgent, LoginLocation,
                        Grid);

                var loginSuccess =
                    await Task.Run(() => _client.Network.Login(libreMetaverseLoginParams));

                if (loginSuccess)
                {
                    IsLoggedIn = true;
                }
                else
                {
                    Log.Error("Failed to login user: {Username}", username);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while logging in user: {Username}", username);
            }*/
        }

        private static void ShowPreferences()
        {
#if ANDROID
            _navigationService.Navigate("preferences");
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
            if (Application.Current?.ApplicationLifetime is not
                IClassicDesktopStyleApplicationLifetime
                desktopLifetime) return;
            Logout();
            desktopLifetime.Shutdown();
        }
    }
}