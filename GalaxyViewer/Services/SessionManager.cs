using System;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using Serilog;

namespace GalaxyViewer
{
    public class SessionManager
    {
        private readonly ILiteDbService _liteDbService;
        private SessionModel _session;

        public event EventHandler<SessionModel> SessionChanged;

        public SessionManager(ILiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
            _session = _liteDbService.GetSession();
        }

        public SessionModel Session
        {
            get => _session;
            set
            {
                if (_session != value)
                {
                    _session = value;
                    _liteDbService.SaveSession(_session);
                    OnSessionChanged();
                }
            }
        }

        protected virtual void OnSessionChanged()
        {
            SessionChanged?.Invoke(this, _session);
            Log.Information("Session changed: {@Session}", _session);
        }
    }
}