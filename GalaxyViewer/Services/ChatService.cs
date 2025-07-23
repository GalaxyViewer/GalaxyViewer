using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using GalaxyViewer.Models;
using OpenMetaverse;
using Serilog;
using ChatType = OpenMetaverse.ChatType;

namespace GalaxyViewer.Services;

public sealed class ChatService : IDisposable
{
    private readonly GridClient _client;
    private readonly LiteDbService _dbService;
    private bool _disposed;

    private bool
        _hasShownConnectionMessage;

    public ObservableCollection<ChatConversation> Conversations { get; } = [];
    public ChatConversation? LocalChatConversation { get; private set; }

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<ChatConversation>? ConversationUpdated;
    public event EventHandler<ChatConversation?>? ActiveConversationChanged;

    private readonly Dictionary<UUID, Queue<(InstantMessageEventArgs Args, DateTime Timestamp)>> _pendingGroupMessages = new();

    public ChatService(GridClient client, LiteDbService dbService)
    {
        _client = client;
        _dbService = dbService;
        InitializeLocalChat();
        RegisterClientEvents();

        _client.Network.EventQueueRunning += OnNetworkConnected;
        _client.Network.LoginProgress += OnLoginProgress;
    }

    private void InitializeLocalChat()
    {
        LocalChatConversation = new ChatConversation
        {
            Name = "Local Chat",
            MessageType = ChatMessageType.LocalChat,
            IsActive = true
        };

        Dispatcher.UIThread.Post(() => { Conversations.Add(LocalChatConversation); });
    }

    private void RegisterClientEvents()
    {
        UnregisterClientEvents();

        _client.Self.ChatFromSimulator += OnChatFromSimulator;
        _client.Self.IM += OnInstantMessage;
    }

    private void UnregisterClientEvents()
    {
        _client.Self.ChatFromSimulator -= OnChatFromSimulator;
        _client.Self.IM -= OnInstantMessage;
        _client.Network.EventQueueRunning -= OnNetworkConnected;
        _client.Network.LoginProgress -= OnLoginProgress;
    }

    private void OnChatFromSimulator(object? sender, ChatEventArgs e)
    {
        if (e.Type is ChatType.StartTyping or ChatType.StopTyping)
        {
            OnLocalChatTyping(e);
            return;
        }

        if (ShouldFilterMessage(e.Message))
        {
            return;
        }

        var message = new ChatMessage
        {
            SenderName = e.FromName,
            SenderUuid = e.SourceID,
            Message = e.Message,
            MessageType = ChatMessageType.LocalChat,
            ChatType = e.Type,
            SourceType = e.SourceType,
            AudibleLevel = e.AudibleLevel,
            IsFromSelf = e.SourceID == _client.Self.AgentID
        };

        if (LocalChatConversation != null) AddMessageToConversation(LocalChatConversation, message);
    }

    private static bool ShouldFilterMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        var systemFilters = Array.Empty<object>();
        // TODO: Populate with actual system filter terms

        // Check if message contains system filter terms
        if (systemFilters.Any(filter => lowerMessage.Contains((string)filter)))
        {
            return true;
        }

        // Filter empty messages
        return string.IsNullOrWhiteSpace(message) || message.Trim().Length < 1;
    }

    private void OnInstantMessage(object? sender, InstantMessageEventArgs eventArgs)
    {
        // Group chats want to be special so we figure that out here, before any others
        if (eventArgs.IM.GroupIM || _client.Groups.GroupName2KeyCache.ContainsKey(eventArgs.IM.IMSessionID))
        {
            HandleGroupIm(eventArgs);
            return;
        }

        switch (eventArgs.IM.Dialog)
        {
            case InstantMessageDialog.MessageFromAgent:
                HandlePersonalIm(eventArgs);
                break;

            case InstantMessageDialog.MessageFromObject:
                HandleObjectIm(eventArgs);
                break;

            case InstantMessageDialog.StartTyping:
                HandleTypingIndicator(eventArgs, true);
                break;

            case InstantMessageDialog.StopTyping:
                HandleTypingIndicator(eventArgs, false);
                break;

            case InstantMessageDialog.FriendshipOffered:
                HandleFriendshipOffer(eventArgs);
                break;

            case InstantMessageDialog.FriendshipAccepted:
                HandleFriendshipAccepted(eventArgs);
                break;

            case InstantMessageDialog.FriendshipDeclined:
                HandleFriendshipDeclined(eventArgs);
                break;

            case InstantMessageDialog.InventoryOffered:
                HandleInventoryOffer(eventArgs);
                break;

            default:
                Log.Information(
                    "Unhandled InstantMessage Dialog: {ImDialog} from {ImFromAgentName}: {ImMessage}",
                    eventArgs.IM.Dialog, eventArgs.IM.FromAgentName, eventArgs.IM.Message);
                break;
        }
    }

    private void HandleGroupIm(InstantMessageEventArgs e)
    {
        var groupId = e.IM.GroupIM ? e.IM.ToAgentID : e.IM.IMSessionID;

        if (!_client.Groups.GroupName2KeyCache.TryGetValue(groupId, out var groupName))
        {

            if (!_pendingGroupMessages.ContainsKey(groupId))
            {
                _pendingGroupMessages[groupId] = new Queue<(InstantMessageEventArgs, DateTime)>();

                EventHandler<GroupNamesEventArgs>? handler = null;
                handler = (_, args) =>
                {
                    if (args.GroupNames.TryGetValue(groupId, out var name))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ProcessQueuedGroupMessages(groupId, name);
                        });
                    }

                    _client.Groups.GroupNamesReply -= handler;
                };

                _client.Groups.GroupNamesReply += handler;
                _client.Groups.RequestGroupName(groupId);
            }

            _pendingGroupMessages[groupId].Enqueue((e, DateTime.Now));
            return;
        }

        var conversation = GetOrCreateGroupConversation(groupId, groupName);
        AddMessageToGroupConversation(conversation, e);
    }

    private void ProcessQueuedGroupMessages(UUID groupId, string groupName)
    {
        if (!_pendingGroupMessages.TryGetValue(groupId, out var messageQueue))
            return;

        var conversation = GetOrCreateGroupConversation(groupId, groupName);

        while (messageQueue.Count > 0)
        {
            var (e, _) = messageQueue.Dequeue();
            AddMessageToGroupConversation(conversation, e);
        }

        _pendingGroupMessages.Remove(groupId);
    }

    private ChatConversation GetOrCreateGroupConversation(UUID groupId, string groupName)
    {
        var conversation = Conversations.FirstOrDefault(c =>
            c.MessageType == ChatMessageType.GroupChat && c.GroupId == groupId);

        if (conversation != null) return conversation;
        conversation = new ChatConversation
        {
            MessageType = ChatMessageType.GroupChat,
            GroupId = groupId,
            GroupName = groupName,
            Name = groupName
        };

        Dispatcher.UIThread.Post(() =>
        {
            Conversations.Add(conversation);
        });

        return conversation;
    }

    private void HandlePersonalIm(InstantMessageEventArgs e)
    {
        var conversation = GetOrCreateImConversation(e.IM.FromAgentID, e.IM.FromAgentName);

        var message = new ChatMessage
        {
            SenderName = e.IM.FromAgentName,
            SenderUuid = e.IM.FromAgentID,
            Message = e.IM.Message,
            MessageType = ChatMessageType.InstantMessage,
            ImDialog = e.IM.Dialog,
            IsFromSelf = e.IM.FromAgentID == _client.Self.AgentID
        };

        AddMessageToConversation(conversation, message);
    }

    private void HandleObjectIm(InstantMessageEventArgs e)
    {
        // Object messages might go to a separate "Objects" conversation for now,
        // but we'd like the ability for it to be filtered or grouped into local chat too
        // TODO: Implement filtering and preferences
        var conversation = GetOrCreateObjectConversation();

        var message = new ChatMessage
        {
            SenderName = e.IM.FromAgentName,
            SenderUuid = e.IM.FromAgentID,
            Message = e.IM.Message,
            MessageType = ChatMessageType.Objects,
            ImDialog = e.IM.Dialog,
            IsFromSelf = false
        };

        AddMessageToConversation(conversation, message);
    }

    private void AddMessageToConversation(ChatConversation conversation, ChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var isDuplicate = conversation.Messages.Any(m =>
                    m.SenderUuid == message.SenderUuid &&
                    m.Message.Equals(message.Message, StringComparison.Ordinal) &&
                    m.MessageType == message.MessageType &&
                    m.Timestamp == message.Timestamp);

                if (isDuplicate)
                {
                    Log.Debug("Skipping duplicate message from {SenderName}: {Message}",
                        message.SenderName, message.Message);
                    return;
                }

                conversation.Messages.Add(message);
                conversation.LastMessage = message.Message;
                conversation.LastActivity = message.Timestamp;

                if (!message.IsFromSelf && !conversation.IsActive)
                {
                    conversation.HasUnreadMessages = true;
                    conversation.UnreadCount++;
                }

                MessageReceived?.Invoke(this, message);
                ConversationUpdated?.Invoke(this, conversation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding message to conversation");
            }
        });
    }

    private void AddMessageToGroupConversation(ChatConversation conversation, InstantMessageEventArgs e)
    {
        var m = new ChatMessage
        {
            SenderName = e.IM.FromAgentName,
            SenderUuid = e.IM.FromAgentID,
            Message = e.IM.Message,
            Timestamp = DateTime.Now,
            MessageType = ChatMessageType.GroupChat,
            GroupId = e.IM.ToAgentID,
            GroupName = conversation.GroupName,
            IsFromSelf = e.IM.FromAgentID == _client.Self.AgentID,
            ImDialog = e.IM.Dialog
        };

        Dispatcher.UIThread.Post(() =>
        {
            conversation.Messages.Add(m);
            conversation.LastMessage = m.Message;
            conversation.LastActivity = m.Timestamp;

            if (!m.IsFromSelf && !conversation.IsActive)
            {
                conversation.HasUnreadMessages = true;
                conversation.UnreadCount++;
            }

            MessageReceived?.Invoke(this, m);
        });
    }

    private void HandleTypingIndicator(InstantMessageEventArgs eventArgs, bool isTyping)
    {
        var conversation = Conversations.FirstOrDefault(c =>
            c.MessageType == ChatMessageType.InstantMessage && c.ParticipantId == eventArgs.IM.FromAgentID);

        if (conversation == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            conversation.IsTyping = isTyping;
            if (isTyping)
            {
                conversation.LastMessage = $"{eventArgs.IM.FromAgentName} is typing...";
            }
            else
            {
                var lastActualMessage = conversation.Messages.LastOrDefault();
                conversation.LastMessage = lastActualMessage?.Message ?? "";
            }
        });

        ConversationUpdated?.Invoke(this, conversation);
    }

    private void HandleFriendshipOffer(InstantMessageEventArgs e)
    {
        // This should trigger a notification/dialog, not create a chat
        // For now, we'll log it
        Log.Information("Friendship offer from {ImFromAgentName}: {ImMessage}", e.IM.FromAgentName,
            e.IM.Message);

        // TODO: Show friendship offer dialog
        // TODO: Allow user to accept/decline
    }

    private void HandleFriendshipAccepted(InstantMessageEventArgs e)
    {
        var conversation = GetOrCreateImConversation(e.IM.FromAgentID, e.IM.FromAgentName);
        var message = new ChatMessage
        {
            SenderName = "System",
            SenderUuid = e.IM.FromAgentID,
            Message = $"{e.IM.FromAgentName} has accepted your friendship request.",
            MessageType = ChatMessageType.InstantMessage,
            ImDialog = e.IM.Dialog,
            IsFromSelf = false
        };
        AddMessageToConversation(conversation, message);
    }

    private void HandleFriendshipDeclined(InstantMessageEventArgs e)
    {
        var conversation = GetOrCreateImConversation(e.IM.FromAgentID, e.IM.FromAgentName);
        var message = new ChatMessage
        {
            SenderName = "System",
            SenderUuid = e.IM.FromAgentID,
            Message = $"{e.IM.FromAgentName} has declined your friendship request.",
            MessageType = ChatMessageType.InstantMessage,
            ImDialog = e.IM.Dialog,
            IsFromSelf = false
        };
        AddMessageToConversation(conversation, message);
    }

    private void HandleInventoryOffer(InstantMessageEventArgs e)
    {
        // This should trigger an inventory offer dialog later, for now we'll log it
        Log.Information("Inventory offer from {ImFromAgentName}: {ImMessage}", e.IM.FromAgentName,
            e.IM.Message);

        // TODO: Show inventory offer dialog
        // TODO: Allow user to accept/decline
    }

    public void StartLocalChatTyping()
    {
        if (!_client.Network.Connected) return;
        _client.Self.AnimationStart(Animations.TYPE, true);
        _client.Self.Chat("", 0, ChatType.StartTyping);
    }

    public void StopLocalChatTyping()
    {
        if (!_client.Network.Connected) return;
        _client.Self.AnimationStop(Animations.TYPE, true);
        _client.Self.Chat("", 0, ChatType.StopTyping);
    }

    private void OnLocalChatTyping(ChatEventArgs e)
    {
        if (LocalChatConversation == null) return;

        var isTyping = e.Type == ChatType.StartTyping;
        var typingUserName = e.FromName;

        if (e.SourceID == _client.Self.AgentID) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (isTyping)
            {
                if (!LocalChatConversation.TypingUsers.Contains(typingUserName))
                {
                    LocalChatConversation.TypingUsers.Add(typingUserName);
                }
            }
            else
            {
                LocalChatConversation.TypingUsers.Remove(typingUserName);
            }

            LocalChatConversation.IsTyping = LocalChatConversation.TypingUsers.Count > 0;

            if (LocalChatConversation.IsTyping)
            {
                var typingMessage = LocalChatConversation.TypingUsers.Count switch
                {
                    1 => $"{LocalChatConversation.TypingUsers.First()} is typing...",
                    2 =>
                        $"{LocalChatConversation.TypingUsers.First()} and {LocalChatConversation.TypingUsers.Last()} are typing...",
                    _ =>
                        $"{LocalChatConversation.TypingUsers.First()} and {LocalChatConversation.TypingUsers.Count - 1} others are typing..."
                };

                LocalChatConversation.LastMessage = typingMessage;
            }
            else
            {
                var lastActualMessage = LocalChatConversation.Messages.LastOrDefault();
                LocalChatConversation.LastMessage = lastActualMessage?.Message ?? "";
            }
        });

        ConversationUpdated?.Invoke(this, LocalChatConversation);
    }

    private ChatConversation GetOrCreateImConversation(UUID participantId, string participantName)
    {
        var existing = Conversations.FirstOrDefault(c =>
            c.MessageType == ChatMessageType.InstantMessage && c.ParticipantId == participantId);

        if (existing != null)
            return existing;

        var conversation = new ChatConversation
        {
            Name = participantName,
            MessageType = ChatMessageType.InstantMessage,
            ParticipantId = participantId
        };

        Dispatcher.UIThread.Post(() => { Conversations.Add(conversation); });

        return conversation;
    }

    private ChatConversation GetOrCreateObjectConversation()
    {
        var existing = Conversations.FirstOrDefault(c =>
            c is { MessageType: ChatMessageType.InstantMessage, Name: "Objects" });

        if (existing != null)
            return existing;

        var conversation = new ChatConversation
        {
            Name = "Objects",
            MessageType = ChatMessageType.InstantMessage,
            ParticipantId = UUID.Zero
        };

        Dispatcher.UIThread.Post(() => { Conversations.Add(conversation); });

        return conversation;
    }

    public async Task SendLocalChatAsync(string message, ChatType chatType = ChatType.Normal)
    {
        await Task.Run(() =>
        {
            _client.Self.Chat(message, 0, chatType);
        });
    }

    public async Task SendInstantMessageAsync(UUID targetId, string message)
    {
        await Task.Run(() => { _client.Self.InstantMessage(targetId, message); });
    }

    public async Task SendGroupMessageAsync(UUID sessionId, string message)
    {
        await Task.Run(() =>
        {
            _client.Self.InstantMessage(
                _client.Self.Name,
                sessionId,
                message,
                sessionId,
                InstantMessageDialog.SessionSend,
                InstantMessageOnline.Offline,
                Vector3.Zero,
                UUID.Zero,
                []
            );
        });
    }

    public void MarkConversationAsRead(ChatConversation conversation)
    {
        conversation.HasUnreadMessages = false;
        conversation.UnreadCount = 0;
        ConversationUpdated?.Invoke(this, conversation);
    }

    public void SetActiveConversation(ChatConversation? conversation)
    {
        foreach (var conv in Conversations)
        {
            conv.IsActive = conv == conversation;
        }

        if (conversation != null)
        {
            MarkConversationAsRead(conversation);
        }

        // Notify that the active conversation has changed
        ActiveConversationChanged?.Invoke(this, conversation);
    }

    private void OnNetworkConnected(object? sender, EventQueueRunningEventArgs e)
    {
        if (_hasShownConnectionMessage) return;
        _hasShownConnectionMessage = true;

        var message = new ChatMessage
        {
            SenderName = "System",
            Message = "Connected to grid successfully.",
            MessageType = ChatMessageType.System,
            IsFromSelf = false,
            Timestamp = DateTime.Now
        };

        if (LocalChatConversation != null) AddMessageToConversation(LocalChatConversation, message);
    }

    private void OnLoginProgress(object? sender, LoginProgressEventArgs e)
    {
        switch (e.Status)
        {
            case LoginStatus.Success:
            {
                var session = _dbService.GetSession();
                if (!string.IsNullOrEmpty(session.LoginWelcomeMessage))
                {
                    var welcomeMessage = new ChatMessage
                    {
                        SenderName = "Grid",
                        Message = session.LoginWelcomeMessage,
                        MessageType = ChatMessageType.System,
                        IsFromSelf = false,
                        Timestamp = DateTime.Now
                    };

                    if (LocalChatConversation != null)
                        AddMessageToConversation(LocalChatConversation, welcomeMessage);
                }

                var loginMessage = new ChatMessage
                {
                    SenderName = "System",
                    Message = $"Successfully logged in as {_client.Self.Name}",
                    MessageType = ChatMessageType.System,
                    IsFromSelf = false,
                    Timestamp = DateTime.Now
                };

                if (LocalChatConversation != null)
                    AddMessageToConversation(LocalChatConversation, loginMessage);
                break;
            }
            case LoginStatus.Failed:
                break;
            case LoginStatus.None:
                break;
            case LoginStatus.ConnectingToLogin:
                break;
            case LoginStatus.ReadingResponse:
                break;
            case LoginStatus.ConnectingToSim:
                break;
            case LoginStatus.Redirecting:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #region Disposable Support

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
            UnregisterClientEvents();
            _pendingGroupMessages.Clear();
            _hasShownConnectionMessage = false;
        }

        _disposed = true;
    }

    ~ChatService()
    {
        Dispose(false);
    }

    #endregion
}