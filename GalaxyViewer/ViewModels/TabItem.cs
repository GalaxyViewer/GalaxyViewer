using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace GalaxyViewer.ViewModels;

public class TabItem : INotifyPropertyChanged
{
    private bool _isActive;
    private bool _hasNotification;
    private int _notificationCount;
    private string _titleResourceKey;
    private object _content;
    private int _chatTabUnreadCount;
    private bool _showChatTabBadge;
    private string _title;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; }

    public string TitleResourceKey
    {
        get => _titleResourceKey;
        set
        {
            _titleResourceKey = value;
            OnPropertyChanged();
        }
    }

    public object Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
            if (value)
            {
                HasNotification = false;
                NotificationCount = 0;
            }
        }
    }

    public bool IsCloseable { get; set; } = true;

    public bool HasNotification
    {
        get => _hasNotification;
        set
        {
            _hasNotification = value;
            OnPropertyChanged();
        }
    }

    public int NotificationCount
    {
        get => _notificationCount;
        set
        {
            _notificationCount = value;
            OnPropertyChanged();
            HasNotification = value > 0;
        }
    }

    public bool HasIcon => IconBrush != null;

    public IBrush? IconBrush { get; set; }

    public int ChatTabUnreadCount
    {
        get => _chatTabUnreadCount;
        set { _chatTabUnreadCount = value; OnPropertyChanged(); }
    }

    public bool ShowChatTabBadge
    {
        get => _showChatTabBadge;
        set { _showChatTabBadge = value; OnPropertyChanged(); }
    }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public TabItem(string id, string titleResourceKey, string title, object content, bool isCloseable = true)
    {
        Id = id;
        TitleResourceKey = titleResourceKey;
        Title = title;
        Content = content;
        IsCloseable = isCloseable;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
