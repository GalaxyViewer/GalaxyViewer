using OpenMetaverse;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace GalaxyViewer.ViewModels
{
    public class LoggedInViewModel : ViewModelBase
    {
        private readonly GridClient _client = new();
        private string _currentLocation;
        private string _currentLocationWelcomeMessage;

        public string CurrentLocation
        {
            get => _currentLocation;
            set
            {
                if (_currentLocation == value) return;
                _currentLocation = value;
            }
        }

        public string CurrentLocationWelcomeMessage
        {
            get => _currentLocationWelcomeMessage;
            set => this.RaiseAndSetIfChanged(ref _currentLocationWelcomeMessage, value);
        }

        public LoggedInViewModel()
        {
            // Initialize CurrentLocation with the current region name
            CurrentLocation = _client.Network.CurrentSim?.Name;
            CurrentLocationWelcomeMessage = $"Welcome to {CurrentLocation}!";
        }
    }
}