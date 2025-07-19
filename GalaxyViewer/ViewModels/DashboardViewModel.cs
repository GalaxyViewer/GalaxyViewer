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

public sealed class DashboardViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly LiteDbService _liteDbService;
    private readonly SessionService _sessionService;
    private readonly GridClient _client;
    private SessionModel _session;

    public int CurrentBalance { get; private set; }
    public AddressBarViewModel AddressBarViewModel { get; }

    private string CurrencySymbol => GetCurrencySymbol();

    public string FormattedBalance => $"{CurrencySymbol}{CurrentBalance:N0}";

    public ReactiveCommand<Unit, Unit> RefreshBalanceCommand { get; }

    public DashboardViewModel(LiteDbService liteDbService, GridClient client,
        SessionService sessionService, ICommand? openPreferencesCommand = null)
    {
        _liteDbService = liteDbService;
        _client = client;
        _sessionService = sessionService;
        _session = _liteDbService.GetSession();
        _liteDbService.PropertyChanged += OnLiteDbServicePropertyChanged;

        AddressBarViewModel = new AddressBarViewModel(_liteDbService, _client, openPreferencesCommand);

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
        Dispatcher.UIThread.Post(() =>
        {
            CurrentBalance = newBalance;
            OnPropertyChanged(nameof(CurrentBalance));
            OnPropertyChanged(nameof(FormattedBalance));
        });
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

    private string GetCurrencySymbol()
    {
        if (_client?.Network?.LoginMessage == null && _client?.Network?.Connected != true)
            return "L$";
        var gridName = _client?.Network?.CurrentSim?.Name ?? "";

        var loginMessage = _client?.Network?.LoginMessage?.ToLowerInvariant() ?? "";

        if (loginMessage.Contains("second life") || loginMessage.Contains("linden") || gridName.Contains("secondlife"))
            return "L$";

        return "L$";
    }
}
