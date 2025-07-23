using System;
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
    public bool IsSystemMessage { get; set; }

    public string MessageTag
    {
        get
        {
            if (MessageType == ChatMessageType.System)
                return "system-message";
            return IsFromSelf ? "from-self" : "from-other";
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