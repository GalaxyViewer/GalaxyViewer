using Avalonia.Controls;
using GalaxyViewer.Views;
using ReactiveUI;
using System.Reactive;

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

        public MainViewModel()
        {
            _currentView = new LoginView();
            IsLoggedIn = false; // Set this to true when the user logs in

            LogoutCommand = ReactiveCommand.Create(Logout);
        }

        private void Logout()
        {
            IsLoggedIn = false;
        }
    }
}