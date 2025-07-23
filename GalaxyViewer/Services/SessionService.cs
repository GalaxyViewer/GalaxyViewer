using System;
using GalaxyViewer.Models;
using OpenMetaverse;

namespace GalaxyViewer.Services;

public class SessionService
{
    private readonly LiteDbService _dbService;

    public SessionService(LiteDbService dbService, GridClient client)
    {
        _dbService = dbService;
        SetClient(client);
    }

    private void SetClient(GridClient client)
    {
        client.Self.MoneyBalance += BalanceReceivedHandler;
    }

    public event EventHandler<int>? BalanceChanged;
    private int _balance;

    public int Balance
    {
        get => _balance;
        private set
        {
            if (_balance == value) return;
            _balance = value;
            BalanceChanged?.Invoke(this, _balance);
        }
    }

    private void BalanceReceivedHandler(object? sender, BalanceEventArgs e)
    {
        Balance = e.Balance;
    }

    public bool HasSessionChanged(SessionModel currentSession)
        => !_dbService.GetSession().Equals(currentSession);
}