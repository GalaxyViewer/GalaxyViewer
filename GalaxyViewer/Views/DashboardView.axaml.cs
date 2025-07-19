using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;
using OpenMetaverse;

namespace GalaxyViewer.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(LiteDbService liteDbService, GridClient gridClient,
        SessionService sessionService, ICommand preferencesCommand = null)
    {
        InitializeComponent();
        DataContext = new DashboardViewModel(liteDbService, gridClient, sessionService, preferencesCommand);
    }
}
