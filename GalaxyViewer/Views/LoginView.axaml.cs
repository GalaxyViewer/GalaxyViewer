using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;
using OpenMetaverse;

namespace GalaxyViewer.Views;

public partial class LoginView : UserControl
{
    public LoginView(LiteDbService liteDbService, GridClient gridClient)
    {
        InitializeComponent();
        DataContext = new LoginViewModel(liteDbService, gridClient);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}