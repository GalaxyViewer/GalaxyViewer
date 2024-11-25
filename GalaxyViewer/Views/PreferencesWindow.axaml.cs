using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public partial class PreferencesWindow : Window
{
    public PreferencesWindow()
    {
        InitializeComponent();
        DataContext = new PreferencesViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}