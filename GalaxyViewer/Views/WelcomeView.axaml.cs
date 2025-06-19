using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OpenMetaverse;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView(LiteDbService liteDbService, GridClient gridClient,
        SessionService sessionService)
    {
        DataContext = new WelcomeViewModel(liteDbService, gridClient, sessionService);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}