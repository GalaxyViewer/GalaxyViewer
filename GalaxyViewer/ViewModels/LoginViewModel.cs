using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using System.Windows.Input;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using GalaxyViewer.Assets.Localization;
using GalaxyViewer.Services;
using Serilog;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels
{
    public class LoginViewModel : ReactiveObject
    {
        private readonly PreferencesViewModel _preferencesViewModel;
        private string _username;
        private string _password;
        private readonly GridClient _client = new();

        public LoginViewModel(PreferencesViewModel preferencesViewModel, string username,
            string password)
        {
            _preferencesViewModel = preferencesViewModel;
            _username = username;
            _password = password;
            LoginLocations = _preferencesViewModel.LoginLocationOptions;
            SelectedLoginLocation = _preferencesViewModel.SelectedLoginLocation;
            TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);

            // Subscribe to the ThrownExceptions property to handle errors
            TryLoginCommand.ThrownExceptions.Subscribe(ex =>
            {
                Log.Error("An error occurred during login: {Error}", ex.Message);
                // Handle the error (e.g., show a dialog to the user)
                _ = ShowLoginErrorAsync("An error occurred during login. Please try again.");
            });
        }

        public ObservableCollection<string> LoginLocations { get; }

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string SelectedLoginLocation
        {
            get => _preferencesViewModel.SelectedLoginLocation;
            set
            {
                _preferencesViewModel.SelectedLoginLocation = value;
                this.RaisePropertyChanged();
            }
        }

        public ReactiveCommand<Unit, Unit> TryLoginCommand { get; }

        private static async Task ShowLoginErrorAsync(string errorMessage)
        {
            var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(
                "Login Error",
                errorMessage,
                ButtonEnum.Ok,
                Icon.Error
            );
            await messageBoxStandardWindow.ShowWindowAsync();
        }

        private async Task TryLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Log.Warning("Username or password is empty");
                await ShowLoginErrorAsync("Username or password is empty");
                return;
            }

            var loginParams = _client.Network.DefaultLoginParams(
                Username.Split(' ')[0], // firstName
                Username.Contains(' ') ? Username.Split(' ')[1] : "Resident", // lastName
                Password,
                "GalaxyViewer", // ViewerName
                "0.1.0" // ViewerVersion
            );

            loginParams.Start =
                _preferencesViewModel
                    .SelectedLoginLocation; // Set the start location to the selected login location

            var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

            if (loginSuccess)
            {
                Log.Information("Login successful");
            }
            else
            {
                Log.Error("Login failed: {Error}", _client.Network.LoginMessage);
                await ShowLoginErrorAsync($"Login failed: {_client.Network.LoginMessage}");
            }
        }
    }
}