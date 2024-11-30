using OpenMetaverse;
using ReactiveUI;
using GalaxyViewer.Services;
using GalaxyViewer.Models;

namespace GalaxyViewer.ViewModels;

public class LoggedInViewModel : ViewModelBase
{
    private readonly LiteDbService _liteDbService;
    private SessionModel _session;

    public string CurrentLocation
    {
        get => _session.CurrentLocation;
        set
        {
            if (_session.CurrentLocation != value)
            {
                _session.CurrentLocation = value;
                _liteDbService.SaveSession(_session);
                var sessionCurrentLocation = _session.CurrentLocation;
                this.RaiseAndSetIfChanged(ref sessionCurrentLocation, value);
            }
        }
    }

    public string CurrentLocationWelcomeMessage
    {
        get => _session.CurrentLocationWelcomeMessage;
        set
        {
            if (_session.CurrentLocationWelcomeMessage != value)
            {
                _session.CurrentLocationWelcomeMessage = value;
                _liteDbService.SaveSession(_session);
                var sessionCurrentLocationWelcomeMessage = _session.CurrentLocationWelcomeMessage;
                this.RaiseAndSetIfChanged(ref sessionCurrentLocationWelcomeMessage, value);
            }
        }
    }
    
    public int Balance
    {
        get => _session.Balance;
        set
        {
            if (_session.Balance != value)
            {
                _session.Balance = value;
                _liteDbService.SaveSession(_session);
                var sessionBalance = _session.Balance;
                this.RaiseAndSetIfChanged(ref sessionBalance, value);
            }
        }
    }

    public LoggedInViewModel(LiteDbService liteDbService)
    {
        _liteDbService = liteDbService;
        _session = _liteDbService.GetSession();

        // Initialize properties with session data
        CurrentLocation = _session.CurrentLocation;
        CurrentLocationWelcomeMessage = _session.CurrentLocationWelcomeMessage;
    }
}