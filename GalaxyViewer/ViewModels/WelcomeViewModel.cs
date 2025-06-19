using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using Avalonia.Threading;
using OpenMetaverse;
using GalaxyViewer.Services;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.ViewModels;

public sealed class WelcomeViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly LiteDbService _liteDbService;
    private readonly SessionService _sessionService;
    private readonly GridClient _client;
    private SessionModel _session;
    private int _balance;

    public int CurrentBalance { get; private set; }

    public ReactiveCommand<Unit, Unit> RefreshBalanceCommand { get; }

    public WelcomeViewModel(LiteDbService liteDbService, GridClient client,
        SessionService sessionService)
    {
        _liteDbService = liteDbService;
        _client = client;
        _sessionService = sessionService;
        _session = _liteDbService.GetSession();
        _liteDbService.PropertyChanged += OnLiteDbServicePropertyChanged;

        _sessionService.BalanceChanged += OnBalanceChanged;
        RefreshBalanceCommand = ReactiveCommand.Create(RequestBalance);

        CurrentBalance = _sessionService.Balance;
        OnPropertyChanged(nameof(CurrentBalance));
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

    private void OnBalanceChanged(object? sender, int newBalance)
    {
        CurrentBalance = newBalance;
    }

    private void RequestBalance()
    {
        try
        {
            _client.Self.RequestBalance();
            Log.Information("Balance request sent to the server.");
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "Failed to request balance from the server.");
        }
    }

    private void OnLiteDbServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(LiteDbService.Session)) return;
        Dispatcher.UIThread.Post(() =>
        {
            _session = _liteDbService.GetSession();
            OnPropertyChanged(nameof(CurrentLocation));
            OnPropertyChanged(nameof(LoginWelcomeMessage));
        });
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    private new void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}