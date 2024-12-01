using GalaxyViewer.Models;
using GalaxyViewer.Services;
using LiteDB;
using System;

namespace GalaxyViewer.Services
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
                _session = value;
                _liteDbService.SaveSession(_session);
                OnSessionChanged(_session);
            }
        }

        protected virtual void OnSessionChanged(SessionModel session)
        {
            SessionChanged?.Invoke(this, session);
        }
    }
}