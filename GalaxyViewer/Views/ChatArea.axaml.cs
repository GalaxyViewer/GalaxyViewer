using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;
using Serilog;
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

        Loaded += (_, _) =>
        {
            _messagesScrollViewer = this.FindControl<ScrollViewer>("MessagesScrollViewer");
        };

        var openChatParticipantsDrawerButton = this.FindControl<Button>("OpenConversationParticipantsDrawerButton");
        if (openChatParticipantsDrawerButton == null) return;
        openChatParticipantsDrawerButton.Click -= OpenConversationParticipantsDrawerButton_Click;
        openChatParticipantsDrawerButton.Click += OpenConversationParticipantsDrawerButton_Click;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _messagesPanel = this.FindControl<StackPanel>("MessagesPanel");

        var openConversationsDrawer = this.FindControl<Button>("OpenDrawerButton");
        if (openConversationsDrawer == null) return;
        openConversationsDrawer.Click += (_, _) => OpenConversationDrawer();
    }

    private void OpenConversationDrawer()
    {
        var options = new DrawerOptions
        {
            Position = Position.Left,
            CanLightDismiss = true,
            IsCloseButtonVisible = true,
            CanResize = false
        };

        const string hostId = "ChatDrawer";

        if (_chatViewModel == null)
        {
            Log.Error("ChatViewModel is null in OpenDrawer. Cannot open drawer.");
            return;
        }

        var drawerViewModel = new ConversationDrawerViewModel(_chatViewModel);

        try
        {
            Drawer.ShowCustom<ConversationDrawerView, ConversationDrawerViewModel>(drawerViewModel,
                hostId, options);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Drawer.ShowCustom threw an exception.");
        }
    }

    private void OpenChatParticipantsDrawer()
    {
        var options = new DrawerOptions
        {
            Position = Position.Right,
            CanLightDismiss = true,
            IsCloseButtonVisible = true,
            CanResize = false
        };

        const string hostId = "ChatParticipantsDrawerHost";

        if (_chatViewModel == null)
        {
            Log.Error("ChatViewModel is null in OpenChatParticipantsDrawer. Cannot open drawer.");
            return;
        }

        var activeConversation = _chatViewModel.ActiveConversation;
        var profileImageService = new ProfileImageService(_chatViewModel.ChatService.Client);
        var drawerViewModel = new ChatParticipantsViewModel(_chatViewModel.ChatService, activeConversation, profileImageService);
        drawerViewModel.SubscribeEvents();

        try
        {
            Drawer.ShowCustom<ChatParticipantsView, ChatParticipantsViewModel>(drawerViewModel,
                hostId, options);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Drawer.ShowCustom threw an exception for ChatParticipantsDrawer.");
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_chatViewModel?.ActiveConversation?.Messages != null)
        {
            _chatViewModel.ActiveConversation.Messages.CollectionChanged -= OnMessagesChanged;
        }

        if (_chatViewModel != null)
        {
            _chatViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _chatViewModel = DataContext as ChatViewModel;

        if (_chatViewModel == null) return;

        if (_chatViewModel.ActiveConversation?.Messages != null)
        {
            _chatViewModel.ActiveConversation.Messages.CollectionChanged += OnMessagesChanged;
        }

        var openDrawerButton = this.FindControl<Button>("OpenDrawerButton");
        if (openDrawerButton != null)
            openDrawerButton.IsEnabled = _chatViewModel != null;

        RefreshMessages();
    }

    private void OnViewModelPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ChatViewModel.ActiveConversation)) return;
        if (_chatViewModel?.ActiveConversation?.Messages != null)
        {
            foreach (var conv in _chatViewModel.Conversations.ToList().Where(conv => conv != _chatViewModel.ActiveConversation))
            {
                conv.Messages.CollectionChanged -= OnMessagesChanged;
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

            // Create a horizontal panel for avatar and sender info
            var senderPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };

            // Add avatar image if available
            if (message.AvatarImage != null)
            {
                var avatarImage = new Image
                {
                    Source = message.AvatarImage,
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                // Make avatar circular
                // TODO: Add a preference for avatar shape
                var avatarBorder = new Border
                {
                    Child = avatarImage,
                    CornerRadius = new CornerRadius(12),
                    ClipToBounds = true,
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                senderPanel.Children.Add(avatarBorder);
            }

            if (!string.IsNullOrEmpty(message.SenderName))
            {
                var senderText = new TextBlock
                {
                    Text = message.SenderName,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                senderPanel.Children.Add(senderText);
            }

            header.Children.Add(senderPanel);

            var timestamp = new TextBlock
            {
                Text = message.Timestamp.ToString("h:mm:ss tt"),
                FontSize = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
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

    private void OpenConversationParticipantsDrawerButton_Click(object? sender, EventArgs e)
    {
        OpenChatParticipantsDrawer();
    }
}