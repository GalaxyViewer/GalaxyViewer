using System;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class SessionModel
{
    public int Id { get; set; }
    public bool IsLoggedIn { get; set; }
    public string AvatarName { get; set; }
    public UUID AvatarKey { get; set; }
    public int Balance { get; set; }
    public string CurrentLocation { get; set; }
    public string LoginWelcomeMessage { get; set; }
}