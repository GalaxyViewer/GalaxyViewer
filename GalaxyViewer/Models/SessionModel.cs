using System;
using System.ComponentModel;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class SessionModel : INotifyPropertyChanged
{
    private int _id;
    private string _avatarName;
    private UUID _avatarKey;
    private string _currentLocation;
    private string _loginWelcomeMessage;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            OnPropertyChanged(nameof(Id));
        }
    }

    public string AvatarName
    {
        get => _avatarName;
        set
        {
            if (_avatarName == value) return;
            _avatarName = value;
            OnPropertyChanged(nameof(AvatarName));
        }
    }

    public UUID AvatarKey
    {
        get => _avatarKey;
        set
        {
            if (_avatarKey == value) return;
            _avatarKey = value;
            OnPropertyChanged(nameof(AvatarKey));
        }
    }

    public string CurrentLocation
    {
        get => _currentLocation;
        set
        {
            if (_currentLocation == value) return;
            _currentLocation = value;
            OnPropertyChanged(nameof(CurrentLocation));
        }
    }

    public string LoginWelcomeMessage
    {
        get => _loginWelcomeMessage;
        set
        {
            if (_loginWelcomeMessage == value) return;
            _loginWelcomeMessage = value;
            OnPropertyChanged(nameof(LoginWelcomeMessage));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}