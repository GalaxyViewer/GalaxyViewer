using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class MenuDesktopView : UserControl
{
    public MenuDesktopView()
    {
        InitializeComponent();
        var liteDbService = new LiteDbService();
        var sessionManager = new SessionManager(liteDbService);
        var gridService = new GridService(liteDbService);
        var preferencesViewModel = new PreferencesViewModel();

        DataContext = new MainViewModel(
            new LoginViewModel(gridService, preferencesViewModel, sessionManager),
            new LoggedInViewModel(liteDbService),
            sessionManager
        );
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}