using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using GalaxyViewer.Views;
using Newtonsoft.Json;
using OpenMetaverse;
using ReactiveUI;
using Ursa.Controls;
using Serilog;

namespace GalaxyViewer.ViewModels;

public class LoginViewModel : ReactiveObject, IRoutableViewModel
{
    public string UrlPathSegment => "login";

    public IScreen HostScreen
    {
        get
        {
            Debug.Assert(_routableViewModelImplementation?.HostScreen != null);
            return _routableViewModelImplementation.HostScreen;
        }
    }

    private string _loginStatusMessage;

    public string LoginStatusMessage
    {
        get => _loginStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _loginStatusMessage, value);
    }

    /*
    public static string SplashScreenUrl
    {
        get
        {
            var version = VersionHelper.GetInformationalVersion();
            var platform = GetFullPlatformString().ToLowerInvariant();
            return $"https://galaxyviewer-splash.pages.dev?version={version}&platform={platform}";
        }
    }

#if DEBUG
    public bool ShowSplashScreen => false;
    // Hide in debug builds because you need a bunch of dependencies for Linux and why would I do all that for a tiny thing?
    // The built one has all the dependencies included so leave it in there
#else
    public bool ShowSplashScreen => true;  // Show in release builds
#endif
*/

    private readonly LiteDbService _liteDbService;
    private readonly PreferencesViewModel _preferencesViewModel;
    private string _username;
    private string _password;
    private readonly GridClient _client;
    private IRoutableViewModel? _routableViewModelImplementation;
    private readonly GridService _gridService;
    private ObservableCollection<GridModel> _grids;
    private GridModel _selectedGrid;

    public WindowToastManager? ToastManager { get; set; }

    private SessionModel _currentSession;

    public SessionModel CurrentSession
    {
        get => _currentSession;
        set => this.RaiseAndSetIfChanged(ref _currentSession, value);
    }

    public LoginViewModel(LiteDbService liteDbService, GridClient client)
    {
        _liteDbService = liteDbService ?? throw new ArgumentNullException(nameof(liteDbService));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _preferencesViewModel = new PreferencesViewModel();
        _currentSession = _liteDbService.GetSession() ??
                          throw new InvalidOperationException("Session could not be retrieved.");
        _username = string.Empty;
        _password = string.Empty;
        LoginLocations = _preferencesViewModel.LoginLocationOptions;
        SelectedLoginLocation = _preferencesViewModel.SelectedLoginLocation;
        TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);
        _gridService = new GridService();
        _grids = [];

        LoadGrids();

        // Subscribe to the ThrownExceptions property to handle errors
        TryLoginCommand.ThrownExceptions.Subscribe(ex =>
        {
            Log.Error("An error occurred during login: {Error}", ex.Message);
            // Handle the error (e.g., show a dialog to the user)
            _ = ShowLoginErrorAsync();
        });

        // Subscribe to the Network.LoginProgress event
        _client.Network.LoginProgress += OnLoginProgress;
    }

    private void UpdateViewBindings()
    {
        // Update the properties bound to the view\
        this.RaisePropertyChanged(nameof(LoginStatusMessage));
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

    private UserControl? _mfaPromptContainer;

    public UserControl? MfaPromptContainer
    {
        get => _mfaPromptContainer;
        set => this.RaiseAndSetIfChanged(ref _mfaPromptContainer, value);
    }

    public ObservableCollection<GridModel> Grids
    {
        get => _grids;
        set => this.RaiseAndSetIfChanged(ref _grids, value);
    }

    private void LoadGrids()
    {
        var grids = _gridService.GetAllGrids();
        Grids = new ObservableCollection<GridModel>(grids);

        // If no selection, default to the Second Life grid
        if (Grids.Count > 0)
        {
            if (string.IsNullOrEmpty(_preferencesViewModel.SelectedGridNick))
                _preferencesViewModel.SelectedGridNick = Grids[0].GridName;

            var selected =
                Grids.FirstOrDefault(g => g.GridName == _preferencesViewModel.SelectedGridNick)
                ?? Grids[0];

            if (SelectedGrid != selected)
                SelectedGrid = selected;
        }
        else
        {
            SelectedGrid = null;
        }
    }

    public GridModel? SelectedGrid
    {
        get => _selectedGrid;
        set
        {
            if (_selectedGrid == value) return;
            _selectedGrid = value;
            if (value != null) _preferencesViewModel.SelectedGridNick = value.GridName;
            this.RaisePropertyChanged();
        }
    }

    public ReactiveCommand<Unit, Unit> TryLoginCommand { get; }

    private async Task ShowLoginErrorAsync()
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
            await ShowLoginErrorAsync();
            return;
        }

        LoginStatusMessage = "Logging in...";

        var ourPlatform = GetPlatformString();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "Version not found";

        var loginParams = _client.Network?.DefaultLoginParams(
            Username.Split(' ')[0], // firstName
            Username.Contains(' ') ? Username.Split(' ')[1] : "Resident", // lastName
            Password,
            "GalaxyViewer", // ViewerName
            version // ViewerVersion
        );

        if (loginParams == null)
        {
            Log.Error("Failed to create login parameters");
            await ShowLoginErrorAsync();
            return;
        }

        loginParams.URI = SelectedGrid?.LoginUri;
        loginParams.MfaEnabled = true;
        loginParams.Platform = ourPlatform;
        loginParams.PlatformVersion = Environment.OSVersion.VersionString;
        loginParams.Start = _preferencesViewModel.SelectedLoginLocation switch
        {
            "Home" => "home",
            "Last Location" => "last",
            _ => "home"
        };
        loginParams.UserAgent = "LibreMetaverse";

        var loginSuccess = await Task.Run(() => _client.Network?.Login(loginParams) ?? false);

        if (loginSuccess)
        {
#if DEBUG
            Log.Information("Login parameters: {LoginParams}",
                JsonConvert.SerializeObject(loginParams, Formatting.Indented));
#endif

            await HandleSuccessfulLogin();
        }
        else if (_client.Network?.LoginMessage.Contains("multifactor") == true)
        {
            Log.Information("MFA required for login");
            var mfaCode = await ShowMfaPromptDialogAsync();
            if (string.IsNullOrWhiteSpace(mfaCode))
            {
                Log.Warning("MFA code is empty");
                await ShowLoginErrorAsync();
                return;
            }

            LoginStatusMessage = "Logging in with MFA...";

            loginParams.Token = mfaCode;

#if DEBUG
            Log.Information("Login parameters with MFA: {LoginParams}",
                JsonConvert.SerializeObject(loginParams, Formatting.Indented));
#endif

            loginSuccess = await Task.Run(() => _client.Network?.Login(loginParams) ?? false);

            if (loginSuccess)
            {
                await HandleSuccessfulLogin();
            }
            else
            {
                Log.Error("MFA login failed: {Error}", _client.Network?.LoginMessage);
                await ShowLoginErrorAsync();
            }
        }
        else
        {
            Log.Error("Login failed: {Error}", _client.Network?.LoginMessage);
            await ShowLoginErrorAsync();
        }
    }

    private bool _isMfaPromptVisible;
    public bool IsMfaPromptVisible
    {
        get => _isMfaPromptVisible;
        set => this.RaiseAndSetIfChanged(ref _isMfaPromptVisible, value);
    }

    private async Task<string> ShowMfaPromptDialogAsync()
    {
        var tcs = new TaskCompletionSource<string>();

        var mfaPromptDialog = new MfaPromptDialog
        {
            DataContext = new MfaPromptDialogViewModel(tcs)
        };

        MfaPromptContainer = mfaPromptDialog;
        IsMfaPromptVisible = true;

        var mfaCode = await tcs.Task;

        MfaPromptContainer = null;
        IsMfaPromptVisible = false;

        return mfaCode;
    }

    private static string GetPlatformString()
    {
        var platformMap = new Dictionary<OSPlatform, string>
        {
            { OSPlatform.Windows, "Win" },
            { OSPlatform.Linux, "Lin" },
            { OSPlatform.OSX, "Mac" },
            { OSPlatform.Create("BROWSER"), "Web" },
            { OSPlatform.Create("ANDROID"), "And" },
            { OSPlatform.Create("IOS"), "iOS" }
        };

        return platformMap.FirstOrDefault(kv => RuntimeInformation.IsOSPlatform(kv.Key)).Value ??
               "Unk";
    }

    private static string GetFullPlatformString()
    {
        var platformMap = new Dictionary<OSPlatform, string>
        {
            { OSPlatform.Windows, "Windows" },
            { OSPlatform.Linux, "Linux" },
            { OSPlatform.OSX, "MacOS" },
            { OSPlatform.Create("BROWSER"), "Browser" },
            { OSPlatform.Create("ANDROID"), "Android" },
            { OSPlatform.Create("IOS"), "iOS" }
        };

        return platformMap.FirstOrDefault(kv => RuntimeInformation.IsOSPlatform(kv.Key)).Value ??
               "Unknown";
    }


    private async Task HandleSuccessfulLogin()
    {
        Log.Information("Login successful as {Name}", _client.Self.Name);
        App.IsLoggedIn = true;

        var session = new SessionModel
        {
            Id = 1,
            AvatarName = _client.Self.Name,
            AvatarKey = _client.Self.AgentID,
            CurrentLocation = _client.Network.CurrentSim.Name,
            LoginWelcomeMessage = _client.Network.LoginMessage
        };

        await _liteDbService.SaveSessionAsync(session);
        CurrentSession = session;
        UpdateViewBindings();

        Log.Information("Session updated on successful login");

        try
        {
            _client.Self.RequestBalance();
            Log.Information("Balance request sent after successful login");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to request balance after login");
        }

        await ProcessCapabilitiesAsync();
    }

    private async Task ProcessCapabilitiesAsync()
    {
        var loginMessage = await Task.Run(() => _client.Network?.LoginMessage);
        Log.Information("Login message: {Message}", loginMessage);

        var currentSim = await Task.Run(() => _client.Network?.CurrentSim);
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
}