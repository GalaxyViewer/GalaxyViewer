using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoginButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ((ViewModels.MainViewModel)DataContext!).IsLoggedIn = true;
        }

    }
}