using System.ComponentModel;
using ReactiveUI;
using GalaxyViewer.Services;
using GalaxyViewer.Models;

namespace GalaxyViewer.ViewModels;

public class LoggedInViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly LiteDbService _liteDbService;
    private SessionModel _session;

    public LoggedInViewModel(LiteDbService liteDbService)
    {
        _liteDbService = liteDbService;
        _session = _liteDbService.GetSession();
        _liteDbService.PropertyChanged += OnLiteDbServicePropertyChanged;
    }

    public string CurrentLocation
    {
        get => _session.CurrentLocation;
        set
        {
            if (_session.CurrentLocation != value)
            {
                _session.CurrentLocation = value;
                _liteDbService.SaveSession(_session);
                OnPropertyChanged(nameof(CurrentLocation));
            }
        }
    }

    public string LoginWelcomeMessage
    {
        get => _session.LoginWelcomeMessage;
        set
        {
            if (_session.LoginWelcomeMessage != value)
            {
                _session.LoginWelcomeMessage = value;
                _liteDbService.SaveSession(_session);
                OnPropertyChanged(nameof(LoginWelcomeMessage));
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
                OnPropertyChanged(nameof(Balance));
            }
        }
    }

    private void OnLiteDbServicePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LiteDbService.Session))
        {
            _session = _liteDbService.GetSession();
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(LoginWelcomeMessage));
            OnPropertyChanged(nameof(Balance));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}