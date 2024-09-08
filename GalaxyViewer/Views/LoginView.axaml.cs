using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Assets.Localization;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            var preferencesViewModel = new PreferencesViewModel();
            DataContext = new LoginViewModel(
                preferencesViewModel,
                string.Empty,
                string.Empty
            );
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}