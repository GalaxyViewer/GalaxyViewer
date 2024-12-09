using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class LoginView : UserControl
{
    private LiteDbService _liteDbService;

    public LoginView()
    {
        InitializeComponent();
        DataContext = new LoginViewModel(_liteDbService);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}