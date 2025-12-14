using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class ChatParticipantsView : UserControl
{
    private ChatParticipantsViewModel? _viewModel;

    public ChatParticipantsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _viewModel = DataContext as ChatParticipantsViewModel;
        _viewModel?.SubscribeEvents();
        _viewModel?.RefreshParticipants();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _viewModel?.UnsubscribeEvents();
    }
}
