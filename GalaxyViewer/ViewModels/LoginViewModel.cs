using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System.Windows.Input;
using GalaxyViewer.Assets.Localization;
using GalaxyViewer.Services;
using Serilog;
using MsBox.Avalonia;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels;

public class LoginViewModel : ReactiveObject
{
    private readonly PreferencesViewModel _preferencesViewModel;
    private string _username;
    private string _password;
    private readonly GridClient _client = new();

    public LoginViewModel(LocalizationManager localizationManager,
        PreferencesViewModel preferencesViewModel, string username, string password)
    {
        _preferencesViewModel = preferencesViewModel;
        _username = username;
        _password = password;
        LoginLocations = _preferencesViewModel.LoginLocationOptions;
        SelectedLoginLocation = _preferencesViewModel.SelectedLoginLocation;
        TryLoginCommand = ReactiveCommand.CreateFromTask(TryLoginAsync);
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
            this.RaisePropertyChanged(nameof(SelectedLoginLocation));
        }
    }

    public ICommand TryLoginCommand { get; }

    private async Task TryLoginAsync()
    {
        var loginParams = _client.Network.DefaultLoginParams(
            Username.Split(' ')[0], // firstName
            Username.Contains(' ') ? Username.Split(' ')[1] : "Resident", // lastName
            Password,
            "GalaxyViewer", // ViewerName
            "0.1" // ViewerVersion
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
        }
    }
}