using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class ChatParticipant : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private Bitmap? _avatarImage;
    private double _distance;
    private UUID _agentId;
    private string _displayName = string.Empty;

    public UUID AgentId
    {
        get => _agentId;
        set { _agentId = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public Bitmap? AvatarImage
    {
        get => _avatarImage;
        set { _avatarImage = value; OnPropertyChanged(); }
    }

    public double Distance
    {
        get => _distance;
        set { _distance = value; OnPropertyChanged(); }
    }

    public string DisplayName
    {
        get => _displayName;
        set { _displayName = value; OnPropertyChanged(); }
    }

    public bool IsOwner { get; set; }
    public bool IsModerator { get; set; }
    public bool IsOnline { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
