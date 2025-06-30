using System;
using GalaxyViewer.Models;
using OpenMetaverse;

namespace GalaxyViewer.Services;

public class SessionService
{
    private readonly LiteDbService _dbService;
    private GridClient _client;

    public SessionService(LiteDbService dbService, GridClient client)
    {
        _dbService = dbService;
        _client = client;
        SetClient(client);
    }

    private void SetClient(GridClient client)
    {
        _client = client;
        RegisterClientEvents(_client);
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

    private void RegisterClientEvents(GridClient client)
    {
        client.Self.ChatFromSimulator += OnChatFromSimulator;
        client.Self.MoneyBalance += BalanceReceivedHandler;
    }

    private void UnregisterClientEvents(GridClient client)
    {
        client.Self.ChatFromSimulator -= OnChatFromSimulator;
        client.Self.MoneyBalance -= BalanceReceivedHandler;
    }

    private void OnChatFromSimulator(object? sender, ChatEventArgs e)
    {
        Console.WriteLine($@"Chat from {e.FromName}: {e.Message}");
    }

    private void BalanceReceivedHandler(object? sender, BalanceEventArgs e)
    {
        Balance = e.Balance;
    }

    public bool HasSessionChanged(SessionModel currentSession)
        => !_dbService.GetSession().Equals(currentSession);
}