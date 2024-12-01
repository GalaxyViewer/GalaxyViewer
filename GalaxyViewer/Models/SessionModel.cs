using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class SessionModel : INotifyPropertyChanged
{
    [BsonId] public ObjectId Id { get; set; }

    private bool _isLoggedIn;
    private string _avatarName;
    private UUID _avatarKey;
    private int _balance;
    private string _currentLocation;
    private string _currentLocationWelcomeMessage;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetField(ref _isLoggedIn, value);
    }

    public string AvatarName
    {
        get => _avatarName;
        set => SetField(ref _avatarName, value);
    }

    public UUID AvatarKey
    {
        get => _avatarKey;
        set => SetField(ref _avatarKey, value);
    }

    public int Balance
    {
        get => _balance;
        set => SetField(ref _balance, value);
    }

    public string CurrentLocation
    {
        get => _currentLocation;
        set => SetField(ref _currentLocation, value);
    }

    public string CurrentLocationWelcomeMessage
    {
        get => _currentLocationWelcomeMessage;
        set => SetField(ref _currentLocationWelcomeMessage, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}