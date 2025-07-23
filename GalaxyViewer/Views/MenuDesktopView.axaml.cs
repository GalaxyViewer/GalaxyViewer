using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views;

public partial class MenuDesktopView : UserControl
{
    public MenuDesktopView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}