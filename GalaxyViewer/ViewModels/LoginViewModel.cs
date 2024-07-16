using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ReactiveUI;
using System.ComponentModel;
using GalaxyViewer.Assets.Localization;
using Serilog;
using OpenMetaverse;

namespace GalaxyViewer.ViewModels;

public abstract partial class LoginViewModel : ReactiveObject
{
    private string _username;
    private string _password;
    private readonly GridClient _client = new GridClient();
    private readonly LocalizationManager _localizationManager;

    protected LoginViewModel(string username, string password,
        LocalizationManager localizationManager)
    {
        _username = username;
        _password = password;
        _localizationManager = localizationManager;
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

    private async void TryLoginAsync()
    {
        /*var loginParams = new LoginParams(
            _client,
            MyRegex().Split(Username.Trim())[0], // firstName
            MyRegex().Split(Username.Trim()).Length > 1
                ? MyRegex().Split(Username.Trim())[1]
                : "Resident", // lastName
            Password,
            ViewerName,
            ViewerVersion,
            DetermineGridUrl())
        {
            LoginLocation = DetermineStartLocation(),
            AgreeToTos = true // Assuming you have a way to get this value
        };

        var loginSuccess = await Task.Run(() => _client.Network.Login(loginParams));

        if (loginSuccess)
        {
            Log.Information(LoginSuccess);
        }
        else
        {
            Log.Logger.Error(LoginFailed);
        }*/
    }
}