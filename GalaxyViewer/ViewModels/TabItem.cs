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
    private string _title;
    private object _content;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
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

    public TabItem(string id, string title, object content, bool isCloseable = true)
    {
        Id = id;
        Title = title;
        Content = content;
        IsCloseable = isCloseable;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}