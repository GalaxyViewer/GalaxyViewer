using System.ComponentModel;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using OpenMetaverse;
using ReactiveUI;

namespace GalaxyViewer.ViewModels
{
    public class LoggedInViewModel : ViewModelBase
    {
        private readonly ILiteDbService _liteDbService;
        private SessionModel _session;

        public LoggedInViewModel(ILiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
            _session = _liteDbService.GetSession();
            _session.PropertyChanged += OnSessionPropertyChanged;
        }

        private void OnSessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SessionModel.AvatarName):
                    this.RaisePropertyChanged(nameof(AvatarName));
                    break;
                case nameof(SessionModel.AvatarKey):
                    this.RaisePropertyChanged(nameof(AvatarKey));
                    break;
                case nameof(SessionModel.Balance):
                    this.RaisePropertyChanged(nameof(Balance));
                    break;
                case nameof(SessionModel.CurrentLocation):
                    this.RaisePropertyChanged(nameof(CurrentLocation));
                    break;
                case nameof(SessionModel.CurrentLocationWelcomeMessage):
                    this.RaisePropertyChanged(nameof(CurrentLocationWelcomeMessage));
                    break;
            }
        }

        public string AvatarName
        {
            get => _session.AvatarName;
            set
            {
                if (_session.AvatarName == value) return;
                _session.AvatarName = value;
                _liteDbService.SaveSession(_session);
                this.RaisePropertyChanged();
            }
        }

        public UUID AvatarKey
        {
            get => _session.AvatarKey;
            set
            {
                if (_session.AvatarKey == value) return;
                _session.AvatarKey = value;
                _liteDbService.SaveSession(_session);
                this.RaisePropertyChanged();
            }
        }

        public int Balance
        {
            get => _session.Balance;
            set
            {
                if (_session.Balance == value) return;
                _session.Balance = value;
                _liteDbService.SaveSession(_session);
                this.RaisePropertyChanged();
            }
        }

        public string CurrentLocation
        {
            get => _session.CurrentLocation;
            set
            {
                if (_session.CurrentLocation == value) return;
                _session.CurrentLocation = value;
                _liteDbService.SaveSession(_session);
                this.RaisePropertyChanged();
            }
        }

        public string CurrentLocationWelcomeMessage
        {
            get => _session.CurrentLocationWelcomeMessage;
            set
            {
                if (_session.CurrentLocationWelcomeMessage == value) return;
                _session.CurrentLocationWelcomeMessage = value;
                _liteDbService.SaveSession(_session);
                this.RaisePropertyChanged();
            }
        }
    }
}