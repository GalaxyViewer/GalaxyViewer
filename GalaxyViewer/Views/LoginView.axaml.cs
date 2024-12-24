using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class LoginView : UserControl
{
    public LoginView(LiteDbService liteDbService)
    {
        InitializeComponent();
        DataContext = new LoginViewModel(liteDbService);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}