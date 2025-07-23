using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class ChatWindow : BaseWindow
{
    public ChatWindow(ChatViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, __) => viewModel.IsInChatWindow = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}