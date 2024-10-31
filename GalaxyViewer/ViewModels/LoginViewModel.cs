﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using ReactiveUI;
using Ursa.Controls;
using Serilog;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels
{
    public class LoginViewModel : ReactiveObject, IRoutableViewModel
    {
        public string UrlPathSegment => "login";
        public IScreen HostScreen
        {
            get
            {
                Debug.Assert(_routableViewModelImplementation?.HostScreen != null, "_routableViewModelImplementation?.HostScreen != null");
                return _routableViewModelImplementation.HostScreen;
            }
        }

        private readonly PreferencesViewModel _preferencesViewModel;
        private string _username;
        private string _password;
        private readonly GridClient _client = new();
        private IRoutableViewModel? _routableViewModelImplementation;

        public WindowToastManager? ToastManager { get; set; }

        public LoginViewModel()
        {
            _preferencesViewModel = new PreferencesViewModel(); // Initialize as needed
            _username = string.Empty;
            _password = string.Empty;
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

        private async Task ShowLoginErrorAsync(string errorMessage)
        {
            ToastManager?.Show(
                new Toast(
                    errorMessage),
                showIcon: true,
                showClose: true,
                type: NotificationType.Error
            );
            await Task.CompletedTask;
        }

        private async Task TryLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Log.Warning("Username or password is empty");
                await ShowLoginErrorAsync("Username or password is empty");
                return;
            }

            var platformMap = new Dictionary<OSPlatform, string>
            {
                { OSPlatform.Windows, "Win" },
                { OSPlatform.Linux, "Lin" },
                { OSPlatform.OSX, "Mac" },
                { OSPlatform.Create("ANDROID"), "And" },
                { OSPlatform.Create("IOS"), "iOS" }
            };

            var ourPlatform =
                platformMap.FirstOrDefault(kv => RuntimeInformation.IsOSPlatform(kv.Key)).Value ??
                "Unk";

            var loginParams = _client.Network.DefaultLoginParams(
                Username.Split(' ')[0], // firstName
                Username.Contains(' ') ? Username.Split(' ')[1] : "Resident", // lastName
                Password,
                "GalaxyViewer", // ViewerName
                "0.1.0" // ViewerVersion
            );

            loginParams.URI =
                "https://login.agni.lindenlab.com/cgi-bin/login.cgi"; // Set the login URI to SL main grid for now TODO: Update to use the selected grid in login menu
            loginParams.MfaEnabled = true; // Inform the server that we support MFA
            loginParams.Platform = ourPlatform; // Set the platform - our operating system
            loginParams.PlatformVersion =
                Environment.OSVersion.VersionString; // Set the platform version

            loginParams.Start =
                _preferencesViewModel
                    .SelectedLoginLocation; // Set the start location to the selected login location
            loginParams.MfaHash = string.Empty; // Clear the MFA hash

            var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

            if (loginSuccess)
            {
                Log.Information("Login successful");
            }
            else if (_client.Network.LoginMessage.Contains("MFA required"))
            {
                Log.Warning("MFA required");
                var mfaCode = await ShowMfaInputDialogAsync();
                if (!string.IsNullOrEmpty(mfaCode))
                {
                    loginParams.MfaHash = Utils.MD5(mfaCode);
                    loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));
                    if (loginSuccess)
                    {
                        Log.Information("Login successful with MFA");
                    }
                    else
                    {
                        Log.Error("Login failed with MFA: {Error}", _client.Network.LoginMessage);
                        await ShowLoginErrorAsync(
                            $"Login failed with MFA: {_client.Network.LoginMessage}");
                    }
                }
                else
                {
                    Log.Warning("MFA code entry was canceled");
                    await ShowLoginErrorAsync("MFA code entry was canceled");
                }
            }
            else
            {
                Log.Error("Login failed: {Error}", _client.Network.LoginMessage);
                await ShowLoginErrorAsync($"Login failed: {_client.Network.LoginMessage}");
            }
        }

        private async Task<string> ShowMfaInputDialogAsync()
        {
            // TODO: Implement MFA input dialog
            return await Task.FromResult(string.Empty);
        }
    }
}