using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using OpenMetaverse;
using ReactiveUI;
using Serilog;

namespace GalaxyViewer.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IGridService _gridService;
        private readonly PreferencesViewModel _preferencesViewModel;
        private readonly SessionManager _sessionManager;
        private readonly GridClient _client;
        private string _username;
        private string _password;
        private ObservableCollection<GridModel> _grids;
        private GridModel _selectedGrid;
        private string _loginStatusMessage;
        private bool _isLoggedIn;

        public LoginViewModel(IGridService gridService, PreferencesViewModel preferencesViewModel,
            SessionManager sessionManager)
        {
            _gridService = gridService;
            _preferencesViewModel = preferencesViewModel;
            _sessionManager = sessionManager;
            _client = new GridClient();
            TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);
            LoadGrids();
            LoginLocations =
                new ObservableCollection<string>(_preferencesViewModel.LoginLocationOptions);
            Grids = new ObservableCollection<GridModel>();
        }

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

        private ObservableCollection<string> _loginLocations;

        public ObservableCollection<string> LoginLocations
        {
            get => _loginLocations;
            set => this.RaiseAndSetIfChanged(ref _loginLocations, value);
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

        public ObservableCollection<GridModel> Grids
        {
            get => _grids;
            set => this.RaiseAndSetIfChanged(ref _grids, value);
        }

        public GridModel SelectedGrid
        {
            get => _selectedGrid;
            set
            {
                if (value == null) return;
                _preferencesViewModel.SelectedGridNick = value.GridNick;
                this.RaiseAndSetIfChanged(ref _selectedGrid, value);
            }
        }

        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _loginStatusMessage, value);
        }

        public bool IsLoggedIn
        {
            get => _sessionManager.Session?.IsLoggedIn ?? false;
            set
            {
                var session = _sessionManager.Session;
                if (session != null)
                {
                    session.IsLoggedIn = value;
                    _sessionManager.Session = session;
                }
            }
        }

        public ReactiveCommand<Unit, Unit> TryLoginCommand { get; }

        private void LoadGrids()
        {
            var grids = _gridService.GetAllGrids();
            Grids = new ObservableCollection<GridModel>(grids);
            SelectedGrid =
                Grids.FirstOrDefault(g => g.GridNick == _preferencesViewModel.SelectedGridNick);
        }

        private async Task ShowLoginErrorAsync(string errorMessage)
        {
            // ToastManager?.Show(
            //     new Toast(
            //         errorMessage),
            //     showIcon: true,
            //     showClose: true,
            //     type: NotificationType.Error
            // );
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

            if (SelectedGrid == null || string.IsNullOrWhiteSpace(SelectedGrid.LoginUri))
            {
                Log.Warning("Selected grid or its login URI is empty");
                await ShowLoginErrorAsync("Selected grid or its login URI is empty");
                return;
            }

            LoginStatusMessage = "Logging in...";

            var ourPlatform = GetPlatformString();

            var loginParams = _client.Network.DefaultLoginParams(
                Username.Split(' ')[0], // firstName
                Username.Contains(' ') ? Username.Split(' ')[1] : "Resident", // lastName
                Password,
                "GalaxyViewer-test", // ViewerName
                "0.1.0" // ViewerVersion
            );

            if (loginParams == null)
            {
                Log.Error("Failed to create login parameters");
                await ShowLoginErrorAsync("Failed to create login parameters");
                return;
            }

            // Use a specific uriString for the testing phase
            var isTestingPhase = true; // Set this flag based on your testing condition
            loginParams.URI = isTestingPhase ? "https://login.agni.lindenlab.com/cgi-bin/login.cgi" : SelectedGrid.LoginUri;
            loginParams.MfaEnabled = true;
            loginParams.Platform = ourPlatform;
            loginParams.PlatformVersion = Environment.OSVersion.VersionString;
            loginParams.Start = _preferencesViewModel?.SelectedLoginLocation switch
            {
                "Home" => "home",
                "Last Location" => "last",
                _ => "home"
            };
            loginParams.MfaHash = string.Empty;

            var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

            if (loginSuccess)
            {
                await HandleSuccessfulLogin();
            }
            else if (_client.Network.LoginMessage.Contains("MFA required"))
            {
                await HandleMfaLogin(loginParams);
            }
            else
            {
                Log.Error("Login failed: {Error}", _client.Network.LoginMessage);
                IsLoggedIn = false;
                await ShowLoginErrorAsync($"Login failed: {_client.Network.LoginMessage}");
            }
        }

        private string GetPlatformString()
        {
            var platformMap = new Dictionary<OSPlatform, string>
            {
                { OSPlatform.Windows, "Win" },
                { OSPlatform.Linux, "Lin" },
                { OSPlatform.OSX, "Mac" },
                { OSPlatform.Create("ANDROID"), "And" },
                { OSPlatform.Create("IOS"), "iOS" }
            };

            return platformMap.FirstOrDefault(kv => RuntimeInformation.IsOSPlatform(kv.Key))
                .Value ?? "Unk";
        }

        private async Task HandleSuccessfulLogin()
        {
            Log.Information("Login successful as {Name}", _client.Self.Name);
            IsLoggedIn = true;
            LoginStatusMessage =
                $"Logged in as {_client.Self.Name}, welcome to {_client.Network.CurrentSim?.Name}";

            // Update session data
            var session = _sessionManager.Session;
            session.IsLoggedIn = true;
            session.AvatarName = _client.Self.Name;
            session.AvatarKey = _client.Self.AgentID;
            session.Balance = _client.Self.Balance;
            session.CurrentLocation = _client.Network.CurrentSim?.Name;
            session.CurrentLocationWelcomeMessage =
                "Welcome to " + _client.Network.CurrentSim?.Name;

            // Save session data to the database once after all properties are updated
            _sessionManager.Session = session;
            Log.Information("Session saved after login: {@Session}", session);

            await ProcessCapabilitiesAsync();
        }

        private async Task HandleMfaLogin(LoginParams loginParams)
        {
            Log.Warning("MFA required");
            var mfaCode = await ShowMfaInputDialogAsync();
            if (!string.IsNullOrEmpty(mfaCode))
            {
                loginParams.MfaHash = Utils.MD5(mfaCode);
                var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));
                if (loginSuccess)
                {
                    await HandleSuccessfulLogin();
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

        private async Task ProcessCapabilitiesAsync()
        {
            var loginMessage = await Task.Run(() => _client.Network.LoginMessage);
            Log.Information("Login message: {Message}", loginMessage);

            var currentSim = await Task.Run(() => _client.Network.CurrentSim);
            Log.Information("Current location: {Sim}", currentSim);

            await Task.CompletedTask;
        }

        private void OnLoginProgress(object? sender, LoginProgressEventArgs e)
        {
            Log.Information("Login progress: {Status}", e.Status);
            switch (e.Status)
            {
                case LoginStatus.ConnectingToLogin:
                    LoginStatusMessage = "Connecting to login server...";
                    break;
                case LoginStatus.ConnectingToSim:
                    LoginStatusMessage = "Connecting to region...";
                    break;
                case LoginStatus.Redirecting:
                    LoginStatusMessage = "Redirecting...";
                    break;
                case LoginStatus.ReadingResponse:
                    LoginStatusMessage = "Reading response...";
                    break;
                case LoginStatus.Success:
                    LoginStatusMessage =
                        $"Logged in as {_client.Self.Name}, welcome to {_client.Network.CurrentSim?.Name}";
                    IsLoggedIn = true;
                    break;
                case LoginStatus.Failed:
                    LoginStatusMessage = $"Login failed: {e.Message}";
                    break;
                case LoginStatus.None:
                default:
                    LoginStatusMessage = $"Unknown login status: {e.Status}";
                    break;
            }
        }

        private async Task<string> ShowMfaInputDialogAsync()
        {
            // TODO: Implement MFA input dialog
            return await Task.FromResult(string.Empty);
        }
    }
}