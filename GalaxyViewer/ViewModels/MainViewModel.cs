using Avalonia.Controls;
using GalaxyViewer.Views;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace GalaxyViewer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl? _currentView;
        private bool _isLoggedIn;

        public UserControl? CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                this.RaiseAndSetIfChanged(ref _isLoggedIn, value);
                CurrentView = value ? (UserControl)new LoggedInView() : new LoginView();
            }
        }

        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginCommand { get; }

        public MainViewModel()
        {
            _currentView = new LoginView();
            IsLoggedIn = false; // Set this to true when the user logs in

            LogoutCommand = ReactiveCommand.Create(Logout);
            LoginCommand = ReactiveCommand.CreateFromTask(Login);
        }

        private void Logout()
        {
            // Perform logout operation here
            IsLoggedIn = false;
        }

        private Task Login()
        {
            // Perform login operation here
            // We will change this function to use async once we have the login logic
            // If login is successful, set IsLoggedIn to true
            IsLoggedIn = true;
            // If it doesn't work, we will return an error message
        }
    }
}