using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using GalaxyViewer.Views;
using ReactiveUI;

namespace GalaxyViewer.ViewModels;

public sealed class ChatViewModel : ViewModelBase, IDisposable
{
    internal readonly ChatService ChatService;
    private readonly ICommand? _backToDashboardCommand;
    private ChatConversation? _activeConversation;
    private string _messageText = string.Empty;
    private bool _isLoading;
    private bool _disposed;

    private System.Timers.Timer? _typingTimer;
    private bool _isCurrentlyTyping;
    private DateTime _lastTypingTime;

    public bool IsInChatWindow { get; set; }

    public ObservableCollection<ChatConversation> Conversations => ChatService.Conversations;

    public ChatConversation? ActiveConversation
    {
        get => _activeConversation;
        set
        {
            if (_activeConversation == value) return;
            _activeConversation = value;
            ChatService.SetActiveConversation(value);
            OnPropertyChanged(nameof(ActiveConversation));
            OnPropertyChanged(nameof(ActiveMessages));
            OnPropertyChanged(nameof(CanSendMessage));
        }
    }

    public ObservableCollection<ChatMessage>? ActiveMessages => _activeConversation?.Messages;

    public string MessageText
    {
        get => _messageText;
        set
        {
            _messageText = value;
            OnPropertyChanged(nameof(MessageText));
            OnPropertyChanged(nameof(CanSendMessage));

            HandleTypingDetection();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool CanSendMessage => !string.IsNullOrWhiteSpace(MessageText) &&
                                  ActiveConversation != null && !IsLoading;

    public bool CanTypeMessage =>
        ActiveConversation != null
        && !IsLoading
        && ActiveConversation.MessageType != ChatMessageType.Objects;

    private bool ShowObjectImWarning => ActiveConversation?.MessageType == ChatMessageType.Objects;

    public string ObjectImWarning => ShowObjectImWarning
        ? Application.Current?.FindResource("Chat_ObjectImWarning") as string ?? string.Empty
        : string.Empty;

    public int TotalUnreadCount => Conversations.Where(c => c != ActiveConversation).Sum(c => c.UnreadCount);
    public bool HasUnreadMessages => TotalUnreadCount > 0;

    public ReactiveCommand<Unit, Unit> SendMessageCommand { get; }
    public ReactiveCommand<ChatConversation, Unit> SelectConversationCommand { get; }
    public ReactiveCommand<Unit, Unit> PopOutChatCommand { get; }

    public ChatViewModel(ChatService chatService, ICommand? backToDashboardCommand = null)
    {
        ChatService = chatService;
        _backToDashboardCommand = backToDashboardCommand;

        ChatService.ActiveConversationChanged += OnActiveConversationChanged;
        ChatService.MessageReceived += OnMessageReceived;
        ChatService.ConversationUpdated += OnConversationUpdated;

        Conversations.CollectionChanged += Conversations_CollectionChanged;

        foreach (var conv in Conversations.ToList())
            conv.PropertyChanged += Conversation_PropertyChanged;

        SendMessageCommand = ReactiveCommand.CreateFromTask(SendMessageAsync);
        SelectConversationCommand = ReactiveCommand.Create<ChatConversation>(SelectConversation);
        PopOutChatCommand = ReactiveCommand.Create(PopOutChat);

        ActiveConversation = ChatService.LocalChatConversation;

        InitializeTypingTimer();
    }

    private void Conversations_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ChatConversation conv in e.NewItems)
                conv.PropertyChanged += Conversation_PropertyChanged;
        if (e.OldItems != null)
            foreach (ChatConversation conv in e.OldItems)
                conv.PropertyChanged -= Conversation_PropertyChanged;
        RaiseUnreadProperties();
    }

    private void Conversation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatConversation.UnreadCount))
            RaiseUnreadProperties();
    }

    private void RaiseUnreadProperties()
    {
        OnPropertyChanged(nameof(TotalUnreadCount));
        OnPropertyChanged(nameof(HasUnreadMessages));
    }

    private void InitializeTypingTimer()
    {
        _typingTimer = new System.Timers.Timer(3000); // 3 seconds
        _typingTimer.Elapsed += OnTypingTimerElapsed;
        _typingTimer.AutoReset = false;
    }

    private async Task SendMessageAsync()
    {
        if (!CanSendMessage || ActiveConversation == null) return;

        StopTyping();

        IsLoading = true;
        var message = MessageText;
        MessageText = string.Empty;

        try
        {
            switch (ActiveConversation.MessageType)
            {
                case ChatMessageType.LocalChat:
                    await ChatService.SendLocalChatAsync(message);
                    break;
                case ChatMessageType.InstantMessage when !false:
                    await ChatService.SendInstantMessageAsync(ActiveConversation.ParticipantId,
                        message);
                    break;
                case ChatMessageType.GroupChat when !false:
                    await ChatService.SendGroupMessageAsync(ActiveConversation.GroupId, message);
                    break;
                case ChatMessageType.System:
                    break;
                case ChatMessageType.Objects:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception)
        {
            MessageText = message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectConversation(ChatConversation conversation)
    {
        ActiveConversation = conversation;
        ChatService.MarkConversationAsRead(conversation);
    }

    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(ActiveMessages));

            MessageReceived?.Invoke(this, message);
        });
    }

    public event EventHandler<ChatMessage>? MessageReceived;

    private void OnConversationUpdated(object? sender, ChatConversation conversation)
    {
        OnPropertyChanged(nameof(Conversations));
    }

    private void OnActiveConversationChanged(object? sender, ChatConversation? conversation)
    {
        Dispatcher.UIThread.Post(() => { ActiveConversation = conversation; });
    }

    private void PopOutChat()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            desktop)
            return;

        IsInChatWindow = true;
        var chatWindow = new ChatWindow(this)
        {
            DataContext = this,
            Title = "GalaxyViewer - Chat",
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        _backToDashboardCommand?.Execute(null);

        if (desktop.MainWindow != null)
        {
            chatWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            chatWindow.Show();
        }
    }

    private void HandleTypingDetection()
    {
        if (ActiveConversation?.MessageType != ChatMessageType.LocalChat)
            return;

        var now = DateTime.Now;
        _lastTypingTime = now;

        if (!string.IsNullOrWhiteSpace(MessageText))
        {
            if (!_isCurrentlyTyping)
            {
                _isCurrentlyTyping = true;
                ChatService.StartLocalChatTyping();
            }

            _typingTimer?.Stop();
            _typingTimer?.Start();
        }
        else
        {
            if (_isCurrentlyTyping)
            {
                StopTyping();
            }
        }
    }

    private void OnTypingTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var timeSinceLastTyping = DateTime.Now - _lastTypingTime;
            if (timeSinceLastTyping.TotalSeconds >= 3)
            {
                StopTyping();
            }
        });
    }

    private void StopTyping()
    {
        if (!_isCurrentlyTyping) return;
        _isCurrentlyTyping = false;
        ChatService.StopLocalChatTyping();
        _typingTimer?.Stop();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            ChatService.ActiveConversationChanged -= OnActiveConversationChanged;
            ChatService.MessageReceived -= OnMessageReceived;
            ChatService.ConversationUpdated -= OnConversationUpdated;
        }

        _disposed = true;
    }

    ~ChatViewModel()
    {
        Dispose(false);
    }
}