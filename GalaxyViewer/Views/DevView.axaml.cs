using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using GalaxyViewer.ViewModels;
using Ursa.Controls;

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