using System.Reactive;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using GalaxyViewer.Services;
using GalaxyViewer.Views;
using OpenMetaverse;
using ReactiveUI;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public bool IsMobile { get; }

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

        public ICommand ShowChatCommand { get; }
        public ICommand ShowPreferencesCommand { get; }
        public ICommand ShowDevViewCommand { get; }

        public ICommand ExitCommand { get; }

        public MainViewModel(NavigationService navigationService)
        {
            IsMobile = RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));

            IsLoggedIn = false; // By default you aren't logged in

            LogoutCommand = ReactiveCommand.Create(Logout);
            LoginCommand = ReactiveCommand.Create(() => navigationService.NavigateTo("login"));

            ShowChatCommand = ReactiveCommand.Create(() =>
                navigationService.NavigateTo("chat"));

            ShowPreferencesCommand = ReactiveCommand.Create(ShowPreferences);
            ShowDevViewCommand = ReactiveCommand.Create(() => navigationService.NavigateTo("debug"));

            ExitCommand = ReactiveCommand.Create(ExitApplication);

            // NavigateTo to login view on startup
            if (!IsLoggedIn)
            {
                navigationService.NavigateTo("login");
            }
        }

        private void Logout()
        {
            // Perform logout operation here
            _client.Network.Logout();
            IsLoggedIn = false;
        }

        private static void ShowPreferences()
        {
#if ANDROID
            _navigationService.NavigateTo("preferences");
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