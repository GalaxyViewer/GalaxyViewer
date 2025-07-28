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

    private string GetResourceString(string key)
    {
        if (Avalonia.Application.Current == null) return key;
        var result = Avalonia.Application.Current.FindResource(key);
        if (result is string str)
            return str;
        return key;
    }

    private void InitializeTabs()
    {
        if (_chatService == null) return;

        // Main Chat/Local Chat tab (non-closeable for now)
        var chatViewModel = new ChatViewModel(_chatService);
        var mainChatTab = new TabItem("main_chat", "Menu_Chat", GetResourceString("Menu_Chat"),
            new ChatView { DataContext = chatViewModel }, false);
        Tabs.Add(mainChatTab);

        // World/Map tab (placeholder)
        var worldTab = new TabItem("world", "Menu_World", GetResourceString("Menu_World"),
            new TextBlock { Text = "World/Map view - Coming Soon" }, true);
        Tabs.Add(worldTab);

        // Inventory tab (placeholder)
        var inventoryTab = new TabItem("inventory", "Menu_Inventory", GetResourceString("Menu_Inventory"),
            new TextBlock { Text = "Inventory view - Coming Soon" }, true);
        Tabs.Add(inventoryTab);

        // People/Friends tab (placeholder)
        var peopleTab = new TabItem("people", "Menu_People", GetResourceString("Menu_People"),
            new TextBlock { Text = "People/Friends view - Coming Soon" }, true);
        Tabs.Add(peopleTab);

        if (Tabs.Count > 0)
            ActiveTab = Tabs[0];

        UpdateChatTabBadge();
    }

    private void ActivateTab(TabItem tab)
    {
        ActiveTab = tab;
        tab.NotificationCount = 0;
        this.RaisePropertyChanged(nameof(ActiveTabContent));
        UpdateUnreadMessageCount();
        UpdateChatTabBadge();
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

    private void UpdateChatTabBadge()
    {
        // Find the chat tab
        var chatTab = Tabs.FirstOrDefault(t => t.TitleResourceKey == "Menu_Chat");
        if (chatTab != null && chatTab.Content is ChatView chatView && chatView.DataContext is ChatViewModel chatVm)
        {
            chatTab.ChatTabUnreadCount = chatVm.TotalUnreadCount;
            chatTab.ShowChatTabBadge = !chatTab.IsActive && chatVm.TotalUnreadCount > 0;
        }
    }

    private void UpdateUnreadMessageCount()
    {
        var totalUnread = Tabs.Sum(tab => tab.NotificationCount);
        TotalUnreadMessages = totalUnread;
        UpdateChatTabBadge();
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
