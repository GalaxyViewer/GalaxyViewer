using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GalaxyViewer.Assets.Localization;
using GalaxyViewer.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Windows;

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
            // debug soi we can see the view
            Debug.WriteLine("LoginView initialized");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // We run TryLoginAsync() and await it to ensure that the login process is completed before proceeding
            // TODO: Implement TryLoginAsync()

            /*
             * The following code is a placeholder for the TryLoginAsync() method.
             * This method should be implemented in the LoginViewModel class.
             * The method should handle the login process and return a boolean value indicating whether the login was successful.
             * The method should also handle any errors that may occur during the login process.
             */
            /*
             if (loginSuccess)
            {
                // If login is successful, we navigate to the MainView
                var mainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }
            else
            {
                // If login is unsuccessful, we show an error message
                MsBoxWindow.Show(
                    // Localize the error message
                    // TODO: Add error message
                );
            }
            */
        }
    }
}