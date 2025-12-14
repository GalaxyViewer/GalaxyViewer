using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using GalaxyViewer.Models;
using OpenMetaverse;
using Serilog;
using System.Collections.Concurrent;
using ChatType = OpenMetaverse.ChatType;

namespace GalaxyViewer.Services;

public sealed class ChatService : IDisposable
{
    private readonly GridClient _client;
    private readonly LiteDbService _dbService;
    private readonly ProfileImageService _profileImageService;
    private bool _disposed;

    public GridClient Client => _client;

    private bool
        _hasShownConnectionMessage;

    public ObservableCollection<ChatConversation> Conversations { get; } = [];
    public ChatConversation? LocalChatConversation { get; private set; }

    public event EventHandler<ChatMessage>? MessageReceived;
    public event EventHandler<ChatConversation>? ConversationUpdated;
    public event EventHandler<ChatConversation?>? ActiveConversationChanged;

    private readonly Dictionary<UUID, Queue<(InstantMessageEventArgs Args, DateTime Timestamp)>>
        _pendingGroupMessages = new();

    private readonly ConcurrentDictionary<UUID, string> _avatarNameCache = new();

    public ChatService(GridClient client, LiteDbService dbService)
    {
        _client = client;
        _dbService = dbService;
        _profileImageService = new ProfileImageService(client);
        InitializeLocalChat();
        RegisterClientEvents();

        _client.Network.EventQueueRunning += OnNetworkConnected;
        _client.Network.LoginProgress += OnLoginProgress;

        // Register for ChatterBoxSessionStartReply CAPS event to capture group chat session IDs
        _client.Network.RegisterEventCallback("ChatterBoxSessionStartReply",
            OnChatterBoxSessionStartReplyCaps);
        // Register for avatar name replies
        _client.Avatars.UUIDNameReply += OnUUIDNameReply;
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

        if (LocalChatConversation != null)
        {
            AddMessageToConversation(LocalChatConversation, message);
            LoadProfileImageForMessage(message);
        }
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
        if (eventArgs.IM.GroupIM ||
            _client.Groups.GroupName2KeyCache.ContainsKey(eventArgs.IM.IMSessionID))
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
        // For group IMs:
        // - If GroupIM flag is true: ToAgentID is the group UUID, IMSessionID is the session ID
        // - If GroupIM flag is false but it's in cache: IMSessionID is BOTH the group UUID AND session ID
        UUID groupId;
        UUID sessionId;

        if (e.IM.GroupIM)
        {
            // Standard group IM: ToAgentID = group, IMSessionID = session
            groupId = e.IM.ToAgentID;
            sessionId = e.IM.IMSessionID;
        }
        else
        {
            // For groups you're already in: IMSessionID serves as both group and session ID
            groupId = e.IM.IMSessionID;
            sessionId = e.IM.IMSessionID; // Use the same UUID for session ID
        }

        Log.Debug(
            "[ChatService] HandleGroupIm: GroupIM={GroupIM}, ToAgentID={ToAgentID}, IMSessionID={IMSessionID}, Derived GroupId={GroupId}, Derived SessionId={SessionId}",
            e.IM.GroupIM, e.IM.ToAgentID, e.IM.IMSessionID, groupId, sessionId);

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

        var conversation = GetOrCreateGroupConversation(groupId, groupName, sessionId);
        AddMessageToGroupConversation(conversation, e);
    }

    private void ProcessQueuedGroupMessages(UUID groupId, string groupName)
    {
        if (!_pendingGroupMessages.TryGetValue(groupId, out var messageQueue))
            return;

        // Get sessionId from the first message in the queue
        UUID sessionId = UUID.Zero;
        if (messageQueue.Count > 0)
        {
            var firstMessage = messageQueue.Peek().Item1;
            if (firstMessage.IM.GroupIM)
            {
                // Standard group IM: IMSessionID is the session ID
                sessionId = firstMessage.IM.IMSessionID;
            }
            else
            {
                // For groups you're already in: IMSessionID serves as both group and session ID
                sessionId = firstMessage.IM.IMSessionID;
            }
        }

        var conversation = GetOrCreateGroupConversation(groupId, groupName, sessionId);

        while (messageQueue.Count > 0)
        {
            var (e, _) = messageQueue.Dequeue();
            AddMessageToGroupConversation(conversation, e);
        }

        _pendingGroupMessages.Remove(groupId);
    }

    private ChatConversation GetOrCreateGroupConversation(UUID groupId, string groupName, UUID sessionId = default)
    {
        var conversation = Conversations.FirstOrDefault(c =>
            c.MessageType == ChatMessageType.GroupChat && c.GroupId == groupId);

        if (conversation != null)
        {
            // Update SessionId if it wasn't set before and we have a valid one now
            if ((conversation.SessionId == null || conversation.SessionId == UUID.Zero) &&
                sessionId != UUID.Zero)
            {
                conversation.SessionId = sessionId;
                Log.Information("[ChatService] Updated SessionId={SessionId} for GroupId={GroupId}",
                    sessionId, groupId);
            }
            return conversation;
        }

        conversation = new ChatConversation
        {
            MessageType = ChatMessageType.GroupChat,
            GroupId = groupId,
            GroupName = groupName,
            Name = groupName,
            SessionId = sessionId != UUID.Zero ? sessionId : null
        };

        Dispatcher.UIThread.Post(() => { Conversations.Add(conversation); });

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

        LoadProfileImageForMessage(message);

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

        LoadProfileImageForMessage(message);
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

    private void AddMessageToGroupConversation(ChatConversation conversation,
        InstantMessageEventArgs e)
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
            // Add message to conversation
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
            c.MessageType == ChatMessageType.InstantMessage &&
            c.ParticipantId == eventArgs.IM.FromAgentID);

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
        LoadProfileImageForMessage(message);
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
        LoadProfileImageForMessage(message);
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
        await Task.Run(() => { _client.Self.Chat(message, 0, chatType); });
    }

    public async Task SendInstantMessageAsync(UUID targetId, string message)
    {
        await Task.Run(() => { _client.Self.InstantMessage(targetId, message); });

        // Add your own message to the conversation
        var conversation = GetOrCreateImConversation(targetId, _client.Self.Name);
        var chatMessage = new ChatMessage
        {
            SenderName = _client.Self.Name,
            SenderUuid = _client.Self.AgentID,
            Message = message,
            MessageType = ChatMessageType.InstantMessage,
            ImDialog = InstantMessageDialog.MessageFromAgent,
            IsFromSelf = true,
            Timestamp = DateTime.Now
        };
        LoadProfileImageForMessage(chatMessage);
        AddMessageToConversation(conversation, chatMessage);
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

    public void RequestGroupMembers(UUID groupId)
    {
        if (groupId == UUID.Zero)
        {
            Log.Warning("[ChatService] Cannot request group members with UUID.Zero");
            return;
        }

        Log.Debug("[ChatService] Requesting group members for GroupId={GroupId}", groupId);
        _client.Groups.RequestGroupMembers(groupId);
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

        LoadProfileImageForMessage(message);

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

                    LoadProfileImageForMessage(welcomeMessage);
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

                LoadProfileImageForMessage(loginMessage);
                if (LocalChatConversation != null)
                    AddMessageToConversation(LocalChatConversation, loginMessage);
                break;
            }
            case LoginStatus.Failed:
            case LoginStatus.None:
            case LoginStatus.ConnectingToLogin:
            case LoginStatus.ReadingResponse:
            case LoginStatus.ConnectingToSim:
            case LoginStatus.Redirecting:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnChatterBoxSessionStartReplyCaps(string capsKey,
        OpenMetaverse.Interfaces.IMessage message, Simulator simulator)
    {
        if (message is not OpenMetaverse.Messages.Linden.ChatterBoxSessionStartReplyMessage
            sessionStart) return;
        var sessionId = sessionStart.SessionID;
        var tempSessionId = sessionStart.TempSessionID;
        var sessionName = sessionStart.SessionName;
        var type = sessionStart.Type;
        var voiceEnabled = sessionStart.VoiceEnabled;
        var moderatedVoice = sessionStart.ModeratedVoice;
        var success = sessionStart.Success;

        Log.Information(
            "[ChatService] ChatterBoxSessionStartReply: SessionId={SessionId}, TempSessionId={TempSessionId}, SessionName={SessionName}, Type={Type}, VoiceEnabled={VoiceEnabled}, ModeratedVoice={ModeratedVoice}, Success={Success}",
            sessionId, tempSessionId, sessionName, type, voiceEnabled, moderatedVoice, success);

        var conversation = Conversations.FirstOrDefault(c =>
            c.MessageType == ChatMessageType.GroupChat &&
            (c.GroupName == sessionName || c.Name == sessionName));

        if (conversation == null)
        {
            // Try to find by GroupId if we can determine it
            UUID groupId = UUID.Zero;
            if (_client.Groups.GroupName2KeyCache.ContainsValue(sessionName))
            {
                groupId = _client.Groups.GroupName2KeyCache.FirstOrDefault(x => x.Value == sessionName).Key;
            }

            if (groupId != UUID.Zero)
            {
                conversation = Conversations.FirstOrDefault(c =>
                    c.MessageType == ChatMessageType.GroupChat && c.GroupId == groupId);
            }

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    MessageType = ChatMessageType.GroupChat,
                    GroupId = groupId,
                    GroupName = sessionName,
                    Name = sessionName,
                    SessionId = sessionId
                };
                Dispatcher.UIThread.Post(() => { Conversations.Add(conversation); });
                Log.Information("[ChatService] Created new conversation for session {SessionName} with SessionId={SessionId}, GroupId={GroupId}",
                    sessionName, sessionId, groupId);
            }
        }

        // Directly update the existing conversation object instead of replacing it
        conversation.SessionId = sessionId;

        // Try to get the groupId from the group name if not already set
        if (conversation.GroupId == UUID.Zero && _client.Groups.GroupName2KeyCache.ContainsValue(sessionName))
        {
            var groupId = _client.Groups.GroupName2KeyCache.FirstOrDefault(x => x.Value == sessionName).Key;
            if (groupId != UUID.Zero)
            {
                conversation.GroupId = groupId;
            }
        }

        Log.Information("[ChatService] Updated SessionId={SessionId} for conversation {ConversationName}, GroupId={GroupId}",
            sessionId, conversation.Name, conversation.GroupId);

        Dispatcher.UIThread.Post(() =>
        {
            ConversationUpdated?.Invoke(this, conversation);
        });
    }

    private void OnUUIDNameReply(object? sender, UUIDNameReplyEventArgs e)
    {
        foreach (var kvp in e.Names)
        {
            _avatarNameCache[kvp.Key] = kvp.Value;
            // Update all conversations that have this participant
            foreach (var conversation in Conversations.ToList())
            {
                var participant =
                    conversation.Participants.FirstOrDefault(p => p.AgentId == kvp.Key);
                if (participant != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        participant.Name = kvp.Value;
                        ConversationUpdated?.Invoke(this, conversation);
                    });
                }
            }
        }
    }

    private void LoadProfileImageForMessage(ChatMessage message)
    {
        if (message.SenderUuid == UUID.Zero || message.MessageType == ChatMessageType.System)
            return;

        // Load profile image asynchronously
        Task.Run(async () =>
        {
            try
            {
                await LoadProfileImageAsync(message);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ChatService] Failed to load profile image for {SenderUuid}", message.SenderUuid);
            }
        });
    }

    private async Task LoadProfileImageAsync(ChatMessage message)
    {
        var senderUuid = message.SenderUuid;
        var tcs = new TaskCompletionSource<AvatarPropertiesReplyEventArgs>();
        EventHandler<AvatarPropertiesReplyEventArgs>? handler = null;

        handler = (sender, e) =>
        {
            if (e.AvatarID != senderUuid) return;
            _client.Avatars.AvatarPropertiesReply -= handler;
            tcs.TrySetResult(e);
        };

        _client.Avatars.AvatarPropertiesReply += handler;
        _client.Avatars.RequestAvatarProperties(senderUuid);

        try
        {
            // Wait for the avatar properties with a timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var avatarProps = await tcs.Task.WaitAsync(cts.Token);

            if (avatarProps.Properties.ProfileImage != UUID.Zero)
            {
                var bitmap = await _profileImageService.GetProfileImageAsync(avatarProps.Properties.ProfileImage, cts.Token);
                if (bitmap != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        message.AvatarImage = bitmap;
                        // Trigger UI refresh by finding the conversation and notifying update
                        var conversation = Conversations.FirstOrDefault(c => c.Messages.Contains(message));
                        if (conversation != null)
                        {
                            ConversationUpdated?.Invoke(this, conversation);
                        }
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("[ChatService] Profile image request timed out for {SenderUuid}", senderUuid);
        }
        finally
        {
            _client.Avatars.AvatarPropertiesReply -= handler;
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
            // Unsubscribe from avatar name reply
            _client.Avatars.UUIDNameReply -= OnUUIDNameReply;
        }

        _disposed = true;
    }

    ~ChatService()
    {
        Dispose(false);
    }

    #endregion
}