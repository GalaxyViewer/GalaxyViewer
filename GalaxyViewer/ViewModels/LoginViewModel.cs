using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ReactiveUI;
using System.ComponentModel;
using GalaxyViewer.Assets.Localization;
using Serilog;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels;

public abstract class LoginViewModel : ReactiveObject
{
    private string _username;
    private string _password;
    private readonly GridClient _client = new GridClient();

    protected LoginViewModel(string username, string password)
    {
        this._username = username;
        this._password = password;
        ReactiveCommand.Create(TryLoginAsync);
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

    private static string DetermineStartLocation()
    {
        // Determine the start location based on the user's preferences
        // This method should return a string representing the start location
        // The start location can be Home, Last Location, a region name, coordinates, or a URL
        throw new System.NotImplementedException();
    }

    private static string DetermineGridUrl()
    {
        // Determine the grid URL based on the user's preferences
        // This method should return a string representing the grid URL
        throw new System.NotImplementedException();
    }

    private async void TryLoginAsync()
    {
        // Split username into first and last names
        var parts = Regex.Split(Username.Trim(), @"[. ]+");
        string firstName, lastName;
        if (parts.Length == 2)
        {
            firstName = parts[0];
            lastName = parts[1];
        }
        else
        {
            firstName = Username.Trim();
            lastName = "Resident";
        }

        // Assuming you have a way to get these values in your AvaloniaUI application
        var agreeToTos = true;
        var startLocation = DetermineStartLocation();
        var gridUrl = DetermineGridUrl();

        var loginUri = string.IsNullOrEmpty(gridUrl) ? Settings.AGNI_LOGIN_SERVER : gridUrl;

        var loginParams = new LoginParams(
            _client,
            firstName,
            lastName,
            Password,
            LocalizationManager.GetString("ViewerName"),
            LocalizationManager.GetString("ViewerVersion"),
            loginUri)
        {
            LoginLocation = startLocation,
            AgreeToTos = agreeToTos
        };

        // Additional login parameters as needed, e.g., MFA hash

        var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

        if (loginSuccess)
        {
            // Send back the data to LoginView to navigate to MainView

            // Log successful login
            Log.Information(LocalizationManager.GetString("LoginSuccess"));
        }
        else
        {
            // Handle failed login
            Log.Logger.Error(LocalizationManager.GetString("LoginFailed"));
        }

        // Save configurations if necessary
    }
}