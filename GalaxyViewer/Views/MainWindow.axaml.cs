using Avalonia;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views;

public partial class MainWindow : BaseWindow
{
    public MainWindow()
    {
        InitializeComponent();
        AttachDevToolsConditional();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AttachDevToolsConditional()
    {
#if DEBUG
        this.AttachDevTools();
#endif
    }
}