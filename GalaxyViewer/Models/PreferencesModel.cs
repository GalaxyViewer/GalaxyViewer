using System;

namespace GalaxyViewer.Models
{
    public enum ThemeOptions
    {
        Light,
        Dark,
        System
    }

    public enum LoginLocationOptions
    {
        Home,
        LastLocation
    }

    [Serializable]
    public class PreferencesModel
    {
        private string _theme = Enum.TryParse(typeof(ThemeOptions), "System", out _) ? "System" : "Light";
        private string _loginLocation = Enum.TryParse(typeof(LoginLocationOptions), "LastLocation", out _) ? "LastLocation" : "Home";
        public long LastSavedEpoch { get; set; } // Hidden from UI, but stored

        public string Theme
        {
            get => _theme;
            set
            {
                if (IsValidTheme(value))
                {
                    _theme = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid theme value: {value}");
                }
            }
        }

        public string LoginLocation
        {
            get => _loginLocation;
            set
            {
                if (IsValidLoginLocation(value))
                {
                    _loginLocation = value;
                }
                else
                {
                    throw new ArgumentException($"Invalid login location value: {value}");
                }
            }
        }

        // Assuming ThemeOptions and LoginLocationOptions are enums or similar
        private bool IsValidTheme(string theme) => Enum.TryParse(typeof(ThemeOptions), theme, out _);
        private bool IsValidLoginLocation(string location) => Enum.TryParse(typeof(LoginLocationOptions), location, out _);
    }
}