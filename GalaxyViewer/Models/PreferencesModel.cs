using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GalaxyViewer.Models
{
    [Serializable]
    public class PreferencesModel
    {
        [XmlIgnore]
        public List<string> ThemeOptions { get; set; } = ["Light", "Dark", "Default"];

        [XmlIgnore]
        public List<string> LoginLocationOptions { get; set; } = ["Home", "Last Location"];

        [XmlIgnore]
        public List<string> FontOptions { get; set; } = ["Inter", "Atkinson Hyperlegible"];

        [XmlIgnore]
        public List<string> LanguageOptions { get; set; } = ["en-US"];

        // Default values
        private string _theme = "Default";
        public string Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                }
            }
        }

        private string _loginLocation = "Home";
        public string LoginLocation
        {
            get => _loginLocation;
            set
            {
                if (_loginLocation != value)
                {
                    _loginLocation = value;
                }
            }
        }

        private string _font = "Atkinson Hyperlegible";
        public string Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value;
                }
            }
        }

        private string _language = "en-US";
        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                }
            }
        }

        public long LastSavedEpoch { get; set; }
    }
}