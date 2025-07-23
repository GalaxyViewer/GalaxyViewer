using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using GalaxyViewer.Models;
using GalaxyViewer.ViewModels;
using Ursa.Controls;
using Ursa.Common;
using Ursa.Controls.Options;

namespace GalaxyViewer.Views;

public partial class ChatArea : UserControl
{
    private StackPanel? _messagesPanel;
    private ScrollViewer? _messagesScrollViewer;
    private ChatViewModel? _chatViewModel;

    public ChatArea()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        Loaded += (s, e) =>
        {
            _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _messagesPanel = this.FindControl<StackPanel>("MessagesPanel");

        var openDrawerButton = this.FindControl<Button>("OpenDrawerButton");
        if (openDrawerButton != null)
            openDrawerButton.Click += (s, e) => OpenDrawer();
    }

    private void OpenDrawer()
    {
        var options = new DrawerOptions
        {
            Position = Position.Left,
            CanLightDismiss = true,
            IsCloseButtonVisible = true,
            Title = "Conversations",
            CanResize = false
        };

        var hostId = "ChatDrawer";
        var drawerViewModel = new ConversationDrawerViewModel(_chatViewModel);

        Drawer.ShowCustom<ConversationDrawerView, ConversationDrawerViewModel>(drawerViewModel,
            hostId, options);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_chatViewModel?.ActiveConversation?.Messages != null)
        {
            _chatViewModel.ActiveConversation.Messages.CollectionChanged -= OnMessagesChanged;
        }

        if (_chatViewModel?.Conversations != null)
        {
            _chatViewModel.Conversations.CollectionChanged -= OnConversationsChanged;
        }

        if (_chatViewModel != null)
        {
            _chatViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _chatViewModel = DataContext as ChatViewModel;

        if (_chatViewModel == null) return;
        _chatViewModel.Conversations.CollectionChanged += OnConversationsChanged;

        _chatViewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_chatViewModel.ActiveConversation?.Messages != null)
        {
            _chatViewModel.ActiveConversation.Messages.CollectionChanged += OnMessagesChanged;
        }

        RefreshMessages();
    }

    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChatViewModel.ActiveConversation)) return;
        if (_chatViewModel?.ActiveConversation?.Messages != null)
        {
            foreach (var conv in _chatViewModel.Conversations)
            {
                if (conv != _chatViewModel.ActiveConversation)
                {
                    conv.Messages.CollectionChanged -= OnMessagesChanged;
                }
            }
        }

        if (_chatViewModel?.ActiveConversation?.Messages != null)
        {
            _chatViewModel.ActiveConversation.Messages.CollectionChanged += OnMessagesChanged;
        }

        RefreshMessages();
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshMessages();
    }

    private void OnConversationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // No specific refresh needed for the message panel here
    }

    private void RefreshMessages()
    {
        if (_chatViewModel?.ActiveConversation?.Messages == null)
            return;

        RefreshMessagePanel(_messagesPanel);

        if (_messagesScrollViewer == null) return;
        double threshold = 50;
        var distanceFromBottom = _messagesScrollViewer.Extent.Height
                                 - _messagesScrollViewer.Offset.Y
                                 - _messagesScrollViewer.Viewport.Height;

        if (distanceFromBottom < threshold)
        {
            Dispatcher.UIThread.Post(() => { _messagesScrollViewer.ScrollToEnd(); });
        }
    }

    private void RefreshMessagePanel(StackPanel? messagesPanel)
    {
        if (messagesPanel == null || _chatViewModel?.ActiveConversation?.Messages == null)
            return;

        messagesPanel.Children.Clear();

        // Show placeholder only if there are no messages
        if (!_chatViewModel.ActiveConversation.Messages.Any())
        {
            var placeholder = new TextBlock
            {
                Text = "Chat messages will appear here...",
                Opacity = 0.5,
                FontStyle = FontStyle.Italic
            };
            messagesPanel.Children.Add(placeholder);
            return;
        }

        // Add each message as a chat bubble
        foreach (var message in _chatViewModel.ActiveConversation.Messages)
        {
            var messageElement = CreateMessageElement(message);
            messagesPanel.Children.Add(messageElement);
        }
    }

    private Control CreateMessageElement(ChatMessage message)
    {
        var border = new Border();
        border.Classes.Add("chat-message-bubble");

        if (message.MessageType == ChatMessageType.System)
            border.Classes.Add("system-message");
        else
            border.Classes.Add(message.IsFromSelf ? "from-self" : "from-other");

        var messagePanel = new StackPanel();

        // Header (only for non-system messages)
        if (message.MessageType != ChatMessageType.System)
        {
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            if (!string.IsNullOrEmpty(message.SenderName))
            {
                var senderText = new TextBlock
                {
                    Text = message.SenderName,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                header.Children.Add(senderText);
            }

            var timestamp = new TextBlock
            {
                Text = message.Timestamp.ToString("h:mm:ss tt"),
                FontSize = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            DockPanel.SetDock(timestamp, Dock.Right);
            header.Children.Add(timestamp);

            messagePanel.Children.Add(header);
        }

        var messageContent = new TextBlock
        {
            Text = message.Message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Margin = new Thickness(0, 4, 0, 0)
        };
        messagePanel.Children.Add(messageContent);

        border.Child = messagePanel;

        border.CornerRadius = message.IsFromSelf
            ? new CornerRadius(12, 12, 8, 12)
            : new CornerRadius(12, 12, 12, 8);

        return border;
    }
}