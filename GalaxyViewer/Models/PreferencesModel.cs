using System;
using System.Collections.Generic;

namespace GalaxyViewer.Models;

[Serializable]
public class PreferencesModel
{
    public string Theme { get; set; } = "Default";
    public string LoginLocation { get; set; } = "Home";
    public string Font { get; set; } = "Atkinson Hyperlegible";
    public string Language { get; set; } = "en-US";
    public long LastSavedEpoch { get; set; }
}

public static class PreferencesOptions
{
    public static readonly List<string> ThemeOptions = ["Light", "Dark", "Default"];
    public static readonly List<string> LoginLocationOptions = ["Home", "Last Location"];
    public static readonly List<string> FontOptions = ["Inter", "Atkinson Hyperlegible"];
    public static readonly List<string> LanguageOptions = ["en-US"];
}