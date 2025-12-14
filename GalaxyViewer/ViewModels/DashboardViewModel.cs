using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using ReactiveUI;
using Avalonia.Controls;
using OpenMetaverse;
using GalaxyViewer.Services;
using GalaxyViewer.Models;
using GalaxyViewer.Views;

namespace GalaxyViewer.ViewModels;

public sealed class DashboardViewModel : ViewModelBase, INotifyPropertyChanged
{

    private readonly LiteDbService _liteDbService;
    private readonly SessionService _sessionService;
    private readonly GridClient _client;
    private readonly ChatService? _chatService;
    private SessionModel _session;
    private TabItem? _activeTab;
    private int _totalUnreadMessages;

    public ObservableCollection<TabItem> Tabs { get; }

    private TabItem? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (_activeTab != null)
                _activeTab.IsActive = false;

            this.RaiseAndSetIfChanged(ref _activeTab, value);

            if (_activeTab != null)
                _activeTab.IsActive = true;
        }
    }

    public object? ActiveTabContent => ActiveTab?.Content;

    public int CurrentBalance { get; private set; }
    public string FormattedBalance => $"{CurrencySymbol}{CurrentBalance:N0}";
    private string CurrencySymbol => "L$";

    public AddressBarViewModel AddressBarViewModel { get; }

    private int TotalUnreadMessages
    {
        get => _totalUnreadMessages;
        set
        {
            if (_totalUnreadMessages == value) return;

            _totalUnreadMessages = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnreadMessages));
            OnPropertyChanged(nameof(UnreadMessagesText));
        }
    }

    public bool HasUnreadMessages => TotalUnreadMessages > 0;
    public string UnreadMessagesText => TotalUnreadMessages > 99 ? "99+" : TotalUnreadMessages.ToString();

    public ReactiveCommand<Unit, Unit> RefreshBalanceCommand { get; }

    public ICommand ActivateTabCommand { get; }
    public ICommand CloseTabCommand { get; }

    public DashboardViewModel(
        LiteDbService liteDbService,
        GridClient client,
        SessionService sessionService,
        ICommand? openPreferencesCommand = null,
        ChatService? chatService = null)
    {
        _liteDbService = liteDbService ?? throw new ArgumentNullException(nameof(liteDbService));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        _chatService = chatService;
        _session = _liteDbService.GetSession();

        Tabs = [];

        AddressBarViewModel = new AddressBarViewModel(_liteDbService, _client, openPreferencesCommand);

        RefreshBalanceCommand = ReactiveCommand.Create(RequestBalance);
        ActivateTabCommand = ReactiveCommand.Create<TabItem>(ActivateTab);
        CloseTabCommand = ReactiveCommand.Create<TabItem>(CloseTab);

        InitializeTabs();
        SubscribeToEvents();

        CurrentBalance = _sessionService.Balance;
        OnPropertyChanged(nameof(CurrentBalance));

        if (_chatService != null)
        {
            UpdateUnreadMessageCount();
        }
    }


    private void InitializeTabs()
    {
        if (_chatService == null) return;

        // Main Chat/Local Chat tab (non-closeable for now)
        var chatViewModel = new ChatViewModel(_chatService);
        var mainChatTab = new TabItem("main_chat", "local_chat_resource_key", "Local Chat",
            new ChatView { DataContext = chatViewModel }, false);
        Tabs.Add(mainChatTab);

        // World/Map tab (placeholder)
        // TODO: Implement actual world/map functionality
        var worldTab = new TabItem("world", "world_resource_key", "World",
            new TextBlock { Text = "World/Map view - Coming Soon" }, false);
        Tabs.Add(worldTab);

        // Inventory tab (placeholder)
        // TODO: Implement actual inventory functionality
        var inventoryTab = new TabItem("inventory", "inventory_resource_key", "Inventory",
            new TextBlock { Text = "Inventory view - Coming Soon" }, false);
        Tabs.Add(inventoryTab);

        // People/Friends tab (placeholder)
        // TODO: Implement actual people/friends functionality
        var peopleTab = new TabItem("people", "people_resource_key", "People",
            new TextBlock { Text = "People/Friends view - Coming Soon" }, false);
        Tabs.Add(peopleTab);

        if (Tabs.Count > 0)
            ActiveTab = Tabs[0];
    }

    private void ActivateTab(TabItem tab)
    {
        ActiveTab = tab;
        tab.NotificationCount = 0;
        this.RaisePropertyChanged(nameof(ActiveTabContent));
        UpdateUnreadMessageCount();
    }

    private void CloseTab(TabItem tab)
    {
        if (!tab.IsCloseable) return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (tab == ActiveTab && Tabs.Count > 0)
        {
            var newActiveIndex = Math.Min(index, Tabs.Count - 1);
            ActiveTab = Tabs[newActiveIndex];
        }

        UpdateUnreadMessageCount();
    }

    private void SubscribeToEvents()
    {
        _client.Network.LoginProgress += OnLoginProgress;
        _client.Network.Disconnected += OnDisconnected;
        _sessionService.BalanceChanged += OnBalanceChanged;
    }

    private void UpdateUnreadMessageCount()
    {
        var totalUnread = Tabs.Sum(tab => tab.NotificationCount);
        TotalUnreadMessages = totalUnread;
    }

    private void OnLoginProgress(object? sender, LoginProgressEventArgs e)
    {
        if (e.Status != LoginStatus.Success) return;
        CurrentBalance = _sessionService.Balance;
        OnPropertyChanged(nameof(CurrentBalance));
        OnPropertyChanged(nameof(FormattedBalance));
    }

    private void OnDisconnected(object? sender, DisconnectedEventArgs e)
    {
        CurrentBalance = 0;
        OnPropertyChanged(nameof(CurrentBalance));
        OnPropertyChanged(nameof(FormattedBalance));
    }

    private void OnBalanceChanged(object? sender, int newBalance)
    {
        CurrentBalance = newBalance;
        OnPropertyChanged(nameof(CurrentBalance));
        OnPropertyChanged(nameof(FormattedBalance));
    }

    private void RequestBalance()
    {
        Task.Run(() => _client.Self.RequestBalance());
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    private new void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
