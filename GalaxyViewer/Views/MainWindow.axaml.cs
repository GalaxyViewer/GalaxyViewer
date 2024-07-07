using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow(bool attachDevTools = false)
    {
        InitializeComponent();

#if DEBUG
        if (attachDevTools)
        {
            this.AttachDevTools();
        }
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}