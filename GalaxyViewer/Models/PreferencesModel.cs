using System;
using System.Threading.Tasks;

namespace GalaxyViewer.Models
{
    public enum ThemeOptions
    {
        Light,
        Dark,
        Default
    }

    public enum LoginLocationOptions
    {
        Home,
        LastLocation
    }

    [Serializable]
    public class PreferencesModel
    {
        private ThemeOptions _theme = ThemeOptions.Default;
        private LoginLocationOptions _loginLocation = LoginLocationOptions.Home;
        public long LastSavedEpoch { get; set; }

        public ThemeOptions Theme
        {
            get => _theme;
            set => _theme = value;
        }

        public LoginLocationOptions LoginLocation
        {
            get => _loginLocation;
            set => _loginLocation = value;
        }

        public static implicit operator PreferencesModel(Task<PreferencesModel> v)
        {
            throw new NotImplementedException();
        }
    }
}