using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

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

        private void LoginButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Your event handling code here, which attempts the Login method
        }
    }
}