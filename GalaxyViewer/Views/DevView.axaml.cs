using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views;

public partial class DevView : UserControl
{
    public DevView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}