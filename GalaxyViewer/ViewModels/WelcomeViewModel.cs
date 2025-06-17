using System.ComponentModel;
using ReactiveUI;
using Avalonia.Threading;
using GalaxyViewer.Services;
using GalaxyViewer.Models;

namespace GalaxyViewer.ViewModels;

public class WelcomeViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly LiteDbService _liteDbService;
    private SessionModel _session;

    public WelcomeViewModel(LiteDbService liteDbService)
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
            if (_session.CurrentLocation == value) return;
            _session.CurrentLocation = value;
            _liteDbService.SaveSession(_session);
            OnPropertyChanged(nameof(CurrentLocation));
        }
    }

    public string LoginWelcomeMessage
    {
        get => _session.LoginWelcomeMessage;
        set
        {
            if (_session.LoginWelcomeMessage == value) return;
            _session.LoginWelcomeMessage = value;
            _liteDbService.SaveSession(_session);
            OnPropertyChanged(nameof(LoginWelcomeMessage));
        }
    }

    public int Balance
    {
        get => _session.Balance;
        set
        {
            if (_session.Balance == value) return;
            _session.Balance = value;
            _liteDbService.SaveSession(_session);
            OnPropertyChanged(nameof(Balance));
        }
    }

    private void OnLiteDbServicePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LiteDbService.Session)) return;
        Dispatcher.UIThread.Post(() =>
        {
            _session = _liteDbService.GetSession();
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(LoginWelcomeMessage));
            OnPropertyChanged(nameof(Balance));
        });
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}