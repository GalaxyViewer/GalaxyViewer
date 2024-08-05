using System;
using System.Reflection;
using OpenMetaverse;

namespace GalaxyViewer.Wrappers
{
    public class AgentManagerWrapper
    {
        public AgentManagerWrapper(AgentManager agentManager)
        {
            var onChatEvent = agentManager.GetType().GetEvent("OnChat", BindingFlags.NonPublic | BindingFlags.Instance);
            if (onChatEvent == null) return;
            var handler = new EventHandler<ChatEventArgs>((sender, e) => OnChat?.Invoke(sender, e));
            onChatEvent.AddEventHandler(agentManager, handler);
        }

        public event EventHandler<ChatEventArgs>? OnChat;
    }
}