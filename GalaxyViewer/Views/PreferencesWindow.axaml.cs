using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views;

public partial class PreferencesWindow : BaseWindow
{
    public PreferencesWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}