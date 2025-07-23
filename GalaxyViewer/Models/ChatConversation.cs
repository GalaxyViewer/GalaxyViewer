using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class ChatConversation : INotifyPropertyChanged
{
    private bool _isTyping;

    public UUID AvatarUuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChatMessageType MessageType { get; init; }
    public UUID ParticipantId { get; init; }
    public UUID GroupId { get; set; }
    public string? GroupName { get; set; }
    public UUID? SessionId { get; init; }
    public string? AvatarImage { get; set; }
    public ObservableCollection<ChatMessage> Messages { get; set; } = [];
    public ObservableCollection<string> TypingUsers { get; set; } = [];

    private DateTime _lastActivity = DateTime.Now;
    private bool _hasUnreadMessages;
    private int _unreadCount;
    private string _lastMessage = string.Empty;
    private bool _isActive;

    public DateTime LastActivity
    {
        get => _lastActivity;
        set
        {
            if (_lastActivity == value) return;
            _lastActivity = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastActivity)));
        }
    }

    public bool HasUnreadMessages
    {
        get => _hasUnreadMessages;
        set
        {
            if (_hasUnreadMessages == value) return;
            _hasUnreadMessages = value;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(HasUnreadMessages)));
        }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (_unreadCount == value) return;
            _unreadCount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnreadCount)));
        }
    }

    public string LastMessage
    {
        get => _lastMessage;
        set
        {
            if (_lastMessage == value) return;
            _lastMessage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastMessage)));
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            if (_isTyping == value) return;
            _isTyping = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTyping)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString()
    {
        return Name;
    }
}