using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class LoggedInView : UserControl
{
    public LoggedInView(LiteDbService liteDbService)
    {
        DataContext = new LoggedInViewModel(liteDbService);
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}