using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using ReactiveUI;
using Ursa.Controls;
using Serilog;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels;

public class LoginViewModel : ReactiveObject, IRoutableViewModel
{
    public string UrlPathSegment => "login";

    public IScreen HostScreen
    {
        get
        {
            Debug.Assert(_routableViewModelImplementation?.HostScreen != null,
                "_routableViewModelImplementation?.HostScreen != null");
            return _routableViewModelImplementation.HostScreen;
        }
    }

    private bool _isLoggedIn;

    public bool IsLoggedIn
    {
        get => App.IsLoggedIn;
        set => App.IsLoggedIn = value;
    }

    private string _loginStatusMessage;

    public string LoginStatusMessage
    {
        get => _loginStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _loginStatusMessage, value);
    }

    private readonly LiteDbService _liteDbService;
    private readonly PreferencesViewModel _preferencesViewModel;
    private string _username;
    private string _password;
    private readonly GridClient _client = new();
    private IRoutableViewModel? _routableViewModelImplementation;
    private readonly GridService _gridService;
    private ObservableCollection<GridModel?> _grids;
    private GridModel _selectedGrid;

    private const string DefaultGridUri =
        "https://login.agni.lindenlab.com/cgi-bin/login.cgi";

    public WindowToastManager? ToastManager { get; set; }

    public LoginViewModel(LiteDbService liteDbService)
    {
        _liteDbService = liteDbService;
        _preferencesViewModel = new PreferencesViewModel();
        _username = string.Empty;
        _password = string.Empty;
        LoginLocations = _preferencesViewModel.LoginLocationOptions;
        SelectedLoginLocation = _preferencesViewModel.SelectedLoginLocation;
        TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);
        _gridService = new GridService();
        _grids = new ObservableCollection<GridModel?>();

        LoadGrids();

        // Subscribe to the ThrownExceptions property to handle errors
        TryLoginCommand.ThrownExceptions.Subscribe(ex =>
        {
            Log.Error("An error occurred during login: {Error}", ex.Message);
            // Handle the error (e.g., show a dialog to the user)
            _ = ShowLoginErrorAsync("An error occurred during login. Please try again.");
        });

        // Subscribe to the Network.LoginProgress event
        _client.Network.LoginProgress += OnLoginProgress;
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

    public ObservableCollection<GridModel> Grids { get; set; }

    public GridModel? SelectedGrid
    {
        get
        {
            var selectedGrid = _grids.FirstOrDefault(g =>
                g.GridNick == _preferencesViewModel.SelectedGridNick);
            if (selectedGrid == null)
            {
                selectedGrid = new GridModel
                {
                    GridNick = "Second Life",
                    LoginUri = DefaultGridUri
                };
            }

            return selectedGrid;
        }
        set
        {
            if (value == null) return;
            _preferencesViewModel.SelectedGridNick = value.GridNick;
            this.RaiseAndSetIfChanged(ref _selectedGrid, value);
        }
    }

    private void LoadGrids()
    {
        var grids = _gridService.GetAllGrids();
        _grids = new ObservableCollection<GridModel?>(grids);
        SelectedGrid = _grids.FirstOrDefault(g =>
            g.GridNick == _preferencesViewModel.SelectedGridNick);
    }

    public ReactiveCommand<Unit, Unit> TryLoginCommand { get; }

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

        LoginStatusMessage = "Logging in...";

        var ourPlatform = GetPlatformString();

        var loginParams = _client?.Network?.DefaultLoginParams(
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

        loginParams.URI = SelectedGrid?.LoginUri;
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

        var loginSuccess = await Task.Run(() => _client?.Network?.Login(loginParams) ?? false);

        if (loginSuccess)
        {
            await HandleSuccessfulLogin();
        }
        else if (_client?.Network?.LoginMessage.Contains("MFA required") == true)
        {
            await HandleMfaLogin(loginParams);
        }
        else
        {
            Log.Error("Login failed: {Error}", _client?.Network?.LoginMessage);
            IsLoggedIn = false;
            await ShowLoginErrorAsync($"Login failed: {_client?.Network?.LoginMessage}");
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

        return platformMap.FirstOrDefault(kv => RuntimeInformation.IsOSPlatform(kv.Key)).Value ??
               "Unk";
    }

    private async Task HandleSuccessfulLogin()
    {
        Log.Information("Login successful as {Name}", _client.Self.Name);
        IsLoggedIn = true;

        var session = new SessionModel
        {
            Id = 1, // Assuming a single session record
            IsLoggedIn = true,
            AvatarName = _client.Self.Name,
            AvatarKey = _client.Self.AgentID,
            Balance = _client.Self.Balance,
            CurrentLocation = _client.Network.CurrentSim.Name,
            CurrentLocationWelcomeMessage = "Welcome to " + _client.Network.CurrentSim.Name
        };

        _liteDbService.SaveSession(session);
        Log.Information("Session updated on successful login");

        await ProcessCapabilitiesAsync();
    }

    private async Task HandleMfaLogin(LoginParams loginParams)
    {
        Log.Warning("MFA required");
        var mfaCode = await ShowMfaInputDialogAsync();
        if (!string.IsNullOrEmpty(mfaCode))
        {
            loginParams.MfaHash = Utils.MD5(mfaCode);
            var loginSuccess = await Task.Run(() => _client?.Network?.Login(loginParams) ?? false);
            if (loginSuccess)
            {
                await HandleSuccessfulLogin();
            }
            else
            {
                Log.Error("Login failed with MFA: {Error}", _client?.Network?.LoginMessage);
                await ShowLoginErrorAsync(
                    $"Login failed with MFA: {_client?.Network?.LoginMessage}");
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
        var loginMessage = await Task.Run(() => _client?.Network?.LoginMessage);
        Log.Information("Login message: {Message}", loginMessage);

        var currentSim = await Task.Run(() => _client?.Network?.CurrentSim);
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