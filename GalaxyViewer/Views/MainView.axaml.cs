using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            var contentControl = this.FindControl<ContentControl>("ContentControl");
            if (contentControl == null) return;
            var navigationService = new NavigationService(contentControl);

            // Register routes
            navigationService.RegisterRoute("login", typeof(LoginView));
            navigationService.RegisterRoute("debug", typeof(DebugView));
            navigationService.RegisterRoute("preferences", typeof(PreferencesView));

            DataContext = new MainViewModel(navigationService);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}