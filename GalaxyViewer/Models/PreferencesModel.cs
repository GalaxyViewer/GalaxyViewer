using System;
using System.Collections.Generic;

namespace GalaxyViewer.Models
{
    [Serializable]
    public class PreferencesModel
    {
        public List<string> ThemeOptions { get; set; } = ["Light", "Dark", "Default"];
        public List<string> LoginLocationOptions { get; set; } = ["Home", "Last Location"];
        public List<string> FontOptions { get; set; } =
            ["Inter", "Atkinson Hyperlegible"];
        public List<string> LanguageOptions { get; set; } = ["en-US"];

        // Default values
        public string Theme { get; set; } = "Default";
        public string LoginLocation { get; set; } = "Home";
        public string Font { get; set; } = "Atkinson Hyperlegible";
        public string Language { get; set; } = "en-US";
        public long LastSavedEpoch { get; set; }
    }
}