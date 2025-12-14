using System;
using Avalonia.Media.Imaging;
using OpenMetaverse;

namespace GalaxyViewer.Models;

public class ChatMessage
{
    public string SenderName { get; set; } = string.Empty;
    public UUID SenderUuid { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public ChatMessageType MessageType { get; set; }
    public ChatType ChatType { get; set; } = ChatType.Normal;
    public ChatSourceType SourceType { get; set; }
    public ChatAudibleLevel AudibleLevel { get; set; }
    public UUID GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsFromSelf { get; set; }
    public InstantMessageDialog? ImDialog { get; set; }
    public Bitmap? AvatarImage { get; set; }

    public string MessageTag
    {
        get
        {
            return MessageType switch
            {
                ChatMessageType.System => "system-message",
                ChatMessageType.Objects => "from-object",
                _ => IsFromSelf ? "from-self" : "from-other"
            };
        }
    }
}

public enum ChatMessageType
{
    LocalChat,
    InstantMessage,
    GroupChat,
    System,
    Objects
}