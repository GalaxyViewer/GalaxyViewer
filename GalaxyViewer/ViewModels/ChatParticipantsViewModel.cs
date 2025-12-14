using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using OpenMetaverse;
using OpenMetaverse.Assets;
using Serilog;
using SkiaSharp;
using Timer = System.Timers.Timer;

namespace GalaxyViewer.ViewModels;

public class ChatParticipantsViewModel : ViewModelBase
{
    private readonly GridClient _client;
    private readonly ChatService _chatService;
    private readonly ProfileImageService _profileImageService;
    public ObservableCollection<ChatParticipant> Participants { get; } = [];
    private readonly object _lock = new();
    private Timer? _refreshTimer;
    private const double RefreshIntervalMs = 5000; // 5 seconds
    private readonly ChatConversation _conversation;
    private bool _isDisposed;
    private bool _eventsSubscribed;

    // Property to control distance visibility - only show for local chat
    public bool ShowDistance => _conversation.MessageType == ChatMessageType.LocalChat;

    public ChatParticipantsViewModel(ChatService chatService, ChatConversation conversation, ProfileImageService profileImageService)
    {
        Log.Debug("[ChatParticipants] Constructor called");
        _chatService = chatService;
        _client = chatService.Client;
        _conversation = conversation;
        _profileImageService = profileImageService;
        _client.Objects.AvatarUpdate += OnAvatarUpdate;
        _client.Objects.KillObject += OnKillObject;

        // For group chats, ensure we're joined to the session (like Radegast does)
        if (conversation.MessageType == ChatMessageType.GroupChat &&
            conversation.SessionId != null &&
            conversation.SessionId != UUID.Zero)
        {
            if (!_client.Self.GroupChatSessions.ContainsKey(conversation.SessionId.Value))
            {
                Log.Information("[ChatParticipants] Joining group chat session {SessionId}", conversation.SessionId);
                _client.Self.RequestJoinGroupChat(conversation.SessionId.Value);
            }
            else
            {
                // Immediately populate from existing session data
                UpdateParticipantListFromSession();
            }
        }

        RefreshParticipants();
    }

    public void RefreshParticipants()
    {
        // Check if disposed first
        if (_isDisposed)
        {
            Log.Debug("[ChatParticipants] RefreshParticipants called after disposal, ignoring");
            return;
        }

        Log.Debug(
            "[ChatParticipants] RefreshParticipants called. Conversation type: {ConversationMessageType}",
            _conversation.MessageType);

        switch (_conversation.MessageType)
        {
            case ChatMessageType.GroupChat:
            {
                if (_conversation.GroupId == UUID.Zero)
                {
                    Log.Warning(
                        "[ChatParticipants] GroupChat: GroupId is UUID.Zero, cannot show participants.");
                    return;
                }

                Log.Information(
                    "[ChatParticipants] Group chat participants will be populated via ChatterBoxSessionAgentListUpdates CAPS messages. GroupId: {ConversationGroupId}, SessionId: {SessionId}",
                    _conversation.GroupId, _conversation.SessionId);
                break;
            }
            case ChatMessageType.InstantMessage:
            {
                lock (_lock)
                {
                    var needsUpdate = Participants.Count != 2 ||
                                      Participants.All(p =>
                                          p.AgentId != _client.Self.AgentID) ||
                                      Participants.All(p =>
                                          p.AgentId != _conversation.ParticipantId);

                    if (!needsUpdate)
                        break;

                    Log.Information(
                        "[ChatParticipants] IM participants: self={SelfAgentId}, other={ConversationParticipantId}",
                        _client.Self.AgentID, _conversation.ParticipantId);

                    Participants.Clear();

                    // Add self
                    Participants.Add(new ChatParticipant
                    {
                        AgentId = _client.Self.AgentID,
                        Name = _client.Self.Name,
                        DisplayName = _client.Self.Name,
                        Distance = 0.0 // Distance not applicable for IM
                    });

                    // Add the other participant if valid and not self
                    if (_conversation.ParticipantId != UUID.Zero &&
                        _conversation.ParticipantId != _client.Self.AgentID)
                    {
                        Participants.Add(new ChatParticipant
                        {
                            AgentId = _conversation.ParticipantId,
                            Name = "...",
                            DisplayName = "...",
                            Distance = 0.0 // Distance not applicable for IM
                        });

                        _client.Avatars.RequestAvatarName(_conversation.ParticipantId);

                        if (_client.Avatars.DisplayNamesAvailable())
                        {
                            _client.Avatars.GetDisplayNames([_conversation.ParticipantId],
                                (success, names, _) =>
                                {
                                    if (!success || names == null) return;
                                    lock (_lock)
                                    {
                                        foreach (var agent in names)
                                        {
                                            var person =
                                                Participants.FirstOrDefault(p =>
                                                    p.AgentId == agent.ID);
                                            if (person != null)
                                                person.DisplayName = agent.DisplayName;
                                        }
                                    }
                                });
                        }
                    }
                }

                break;
            }
            case ChatMessageType.LocalChat:
                // Local chat: show all nearby avatars except self
                var sim = _client.Network.CurrentSim;
                if (sim == null) return;
                var selfId = _client.Self.AgentID;
                var avatars = sim.ObjectsAvatars.Values.Where(a => a.ID != selfId).ToList();
                var avatarIds = avatars.Select(a => a.ID).ToList();
                lock (_lock)
                {
                    // Remove avatars no longer present
                    for (var i = Participants.Count - 1; i >= 0; i--)
                    {
                        if (avatarIds.All(id => id != Participants[i].AgentId))
                            Participants.RemoveAt(i);
                    }

                    // Add or update avatars
                    foreach (var avatar in avatars)
                    {
                        var person = Participants.FirstOrDefault(p => avatar.ID == p.AgentId);
                        if (person == null)
                        {
                            person = new ChatParticipant
                            {
                                AgentId = avatar.ID,
                                Distance = (avatar.Position - sim.Client.Self.SimPosition)
                                    .Length()
                            };
                            Participants.Add(person);
                        }
                        else
                        {
                            person.Distance =
                                (avatar.Position - sim.Client.Self.SimPosition).Length();
                        }
                    }
                }

                // Request legacy names
                _client.Avatars.RequestAvatarNames(avatarIds);
                // Request display names
                if (_client.Avatars.DisplayNamesAvailable())
                {
                    _client.Avatars.GetDisplayNames(avatarIds, (success, names, _) =>
                    {
                        if (!success || names == null) return;
                        lock (_lock)
                        {
                            foreach (var agent in names)
                            {
                                var person =
                                    Participants.FirstOrDefault(p => p.AgentId == agent.ID);
                                if (person != null) person.DisplayName = agent.DisplayName;
                            }
                        }
                    });
                }

                break;
            case ChatMessageType.System:
            case ChatMessageType.Objects:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SubscribeEvents()
    {
        if (_eventsSubscribed)
        {
            Log.Warning("[ChatParticipants] SubscribeEvents called but already subscribed, ignoring");
            return;
        }

        Log.Information("[ChatParticipants] SubscribeEvents called");
        _client.Avatars.UUIDNameReply += Avatars_UUIDNameReply;
        _client.Network.SimChanged += Network_SimChanged;
        _client.Objects.ObjectUpdate += Objects_ObjectUpdate;

        // Use Radegast's approach: subscribe to GroupChat events instead of CAPS
        _client.Self.GroupChatJoined += Self_GroupChatJoined;
        _client.Self.ChatSessionMemberAdded += Self_ChatSessionMemberAdded;
        _client.Self.ChatSessionMemberLeft += Self_ChatSessionMemberLeft;

        // Subscribe to avatar properties for profile images
        _client.Avatars.AvatarPropertiesReply += Avatars_AvatarPropertiesReply;

        // Start periodic refresh timer
        _refreshTimer = new Timer(RefreshIntervalMs);
        _refreshTimer.Elapsed += (_, _) => RefreshParticipants();
        _refreshTimer.AutoReset = true;
        _refreshTimer.Start();

        _eventsSubscribed = true;
    }

    public void UnsubscribeEvents()
    {
        _isDisposed = true;

        _client.Avatars.UUIDNameReply -= Avatars_UUIDNameReply;
        _client.Network.SimChanged -= Network_SimChanged;
        _client.Objects.ObjectUpdate -= Objects_ObjectUpdate;

        // Unsubscribe from GroupChat events
        _client.Self.GroupChatJoined -= Self_GroupChatJoined;
        _client.Self.ChatSessionMemberAdded -= Self_ChatSessionMemberAdded;
        _client.Self.ChatSessionMemberLeft -= Self_ChatSessionMemberLeft;

        // Unsubscribe from avatar properties
        _client.Avatars.AvatarPropertiesReply -= Avatars_AvatarPropertiesReply;
        // Stop and dispose timer
        if (_refreshTimer == null) return;
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
        _refreshTimer = null;
    }

    private void Network_SimChanged(object? sender, SimChangedEventArgs e)
    {
        RefreshParticipants();
    }

    private void Objects_ObjectUpdate(object? sender, PrimEventArgs e)
    {
        if (e.Prim is Avatar)
            RefreshParticipants();
    }

    private void Avatars_UUIDNameReply(object? sender, UUIDNameReplyEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            lock (_lock)
            {
                foreach (var kvp in e.Names)
                {
                    var person = Participants.FirstOrDefault(p => kvp.Key == p.AgentId);
                    if (person != null)
                        person.Name = kvp.Value;
                }
            }
        });
    }

    private void OnAvatarUpdate(object? sender, AvatarUpdateEventArgs e)
    {
        // Only track distance for local chat
        if (_conversation.MessageType != ChatMessageType.LocalChat)
            return;

        if (e.Avatar.ID == _client.Self.AgentID) return; // skip self
        var sim = _client.Network.CurrentSim;
        if (sim == null) return;
        var avatar = e.Avatar;
        var distance = (avatar.Position - sim.Client.Self.SimPosition).Length();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            lock (_lock)
            {
                var person = Participants.FirstOrDefault(p => p.AgentId == avatar.ID);
                if (person == null)
                {
                    person = new ChatParticipant { AgentId = avatar.ID, Distance = distance };
                    Participants.Add(person);
                }
                else
                {
                    person.Distance = distance;
                }
            }
        });
    }

    private void OnKillObject(object? sender, KillObjectEventArgs e)
    {
        var sim = _client.Network.CurrentSim;
        if (sim == null) return;
        // Try to get the avatar by local ID
        if (!sim.ObjectsAvatars.TryGetValue(e.ObjectLocalID, out var avatar)) return;
        var avatarId = avatar.ID;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            lock (_lock)
            {
                for (var i = Participants.Count - 1; i >= 0; i--)
                {
                    if (Participants[i].AgentId == avatarId)
                    {
                        Participants.RemoveAt(i);
                    }
                }
            }
        });
    }


    // Radegast's approach: Use GroupChatSessions instead of CAPS messages
    private void Self_GroupChatJoined(object sender, GroupChatJoinedEventArgs e)
    {
        if (_conversation.MessageType != ChatMessageType.GroupChat ||
            _conversation.SessionId != e.SessionID) return;
        Log.Information("[ChatParticipants] Successfully joined group chat session {SessionId}", e.SessionID);
        UpdateParticipantListFromSession();
    }

    private void Self_ChatSessionMemberAdded(object sender, ChatSessionMemberAddedEventArgs e)
    {
        if (_conversation.MessageType != ChatMessageType.GroupChat ||
            _conversation.SessionId != e.SessionID) return;
        Log.Information("[ChatParticipants] Member {AgentId} added to session {SessionId}", e.AgentID, e.SessionID);
        UpdateParticipantListFromSession();
    }

    private void Self_ChatSessionMemberLeft(object sender, ChatSessionMemberLeftEventArgs e)
    {
        if (_conversation.MessageType != ChatMessageType.GroupChat ||
            _conversation.SessionId != e.SessionID) return;
        Log.Information("[ChatParticipants] Member {AgentId} left session {SessionId}", e.AgentID, e.SessionID);
        UpdateParticipantListFromSession();
    }

    private void Avatars_AvatarPropertiesReply(object sender, AvatarPropertiesReplyEventArgs e)
    {
        if (e.Properties.ProfileImage != UUID.Zero)
        {
            LoadProfileImage(e.AvatarID, e.Properties.ProfileImage);
        }
    }

    private void LoadProfileImage(UUID avatarId, UUID imageId)
    {
        // TODO: Profile image loading is an incomplete placeholder. Need to:
        // - Fix the code so it actually works
        // - Implement caching to avoid re-requesting the same image, along with cache expiration
        // - Add image scaling/resizing for display efficiency

        // Run off the UI thread and send the result back to the UI
        _ = Task.Run(async () =>
        {
            try
            {
                var bitmap = await _profileImageService.GetProfileImageAsync(imageId).ConfigureAwait(false);
                if (bitmap == null)
                {
                    Log.Debug("[ChatParticipants] No profile image available for {AgentId}", avatarId);
                    return;
                }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    lock (_lock)
                    {
                        var participant = Participants.FirstOrDefault(p => p.AgentId == avatarId);
                        if (participant == null) return;
                        participant.AvatarImage = bitmap;
                        Log.Debug("[ChatParticipants] Loaded profile image for {AgentId}", avatarId);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ChatParticipants] Failed to load profile image for {AgentId}", avatarId);
            }
        });
    }

    private void UpdateParticipantListFromSession()
    {
        if (_conversation.SessionId == null || _conversation.SessionId == UUID.Zero)
        {
            Log.Warning("[ChatParticipants] Cannot update participant list - SessionId is null or zero");
            return;
        }

        if (_client.Self.GroupChatSessions.TryGetValue(_conversation.SessionId.Value, out var participants))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                lock (_lock)
                {
                    Participants.Clear();
                    var agentIds = new List<UUID>();

                    foreach (var participant in participants)
                    {
                        var chatParticipant = new ChatParticipant
                        {
                            AgentId = participant.AvatarKey,
                            Name = string.Empty, // Will be populated by name requests
                            DisplayName = string.Empty,
                            IsModerator = participant.IsModerator,
                            Distance = 0.0 // Distance not applicable for group chat, ignored
                        };

                        Participants.Add(chatParticipant);
                        agentIds.Add(participant.AvatarKey);
                    }

                    Log.Information("[ChatParticipants] Updated participant list with {Count} members from GroupChatSessions", Participants.Count);

                    // Request names and profile images for all participants
                    if (agentIds.Count <= 0) return;
                    _client.Avatars.RequestAvatarNames(agentIds);

                    foreach (var agentId in agentIds)
                    {
                        _client.Avatars.RequestAvatarProperties(agentId);
                    }

                    if (!_client.Avatars.DisplayNamesAvailable()) return;
                    const int batchSize = 50;
                    for (var i = 0; i < agentIds.Count; i += batchSize)
                    {
                        var batch = agentIds.Skip(i).Take(batchSize).ToList();
                        _client.Avatars.GetDisplayNames(batch, (success, names, _) =>
                        {
                            if (!success || names == null) return;

                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                lock (_lock)
                                {
                                    foreach (var agent in names)
                                    {
                                        var person = Participants.FirstOrDefault(p => p.AgentId == agent.ID);
                                        if (person != null)
                                            person.DisplayName = agent.DisplayName;
                                    }
                                }
                            });
                        });
                    }
                }
            });
        }
        else
        {
            Log.Warning("[ChatParticipants] SessionId {SessionId} not found in GroupChatSessions", _conversation.SessionId);
        }
    }
}