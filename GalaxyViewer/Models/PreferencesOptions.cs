using System.Collections.Generic;

namespace GalaxyViewer.Models;

public static class PreferencesOptions
{
    public static readonly List<string> ThemeOptions = ["Light", "Dark", "System"];
    public static readonly List<string> LoginLocationOptions = ["Home", "Last Location"];
    public static readonly List<string> FontOptions = ["Inter", "Atkinson Hyperlegible"];
    public static readonly List<string> LanguageOptions = ["en-US"];

    public static readonly List<string> AccentColorOptions = [
        "System Default", // Uses OS accent color
        "Blue", // Classic blue
        "Purple", // Purple accent
        "Teal", // Teal accent
        "Green", // Green accent
        "Orange", // Orange accent
        "Red", // Red accent
        "Pink", // Pink accent
        "Indigo" // Indigo accent
    ];

    private static readonly Dictionary<string, (string Light, string Dark)> AccentColors = new()
    {
        { "System Default", ("SystemAccent", "SystemAccent") }, // Special key for system accent
        { "Blue", ("#0078D4", "#60CDFF") }, // Light blue -> Bright blue for dark mode
        { "Purple", ("#8B5A9F", "#B19CD9") }, // Purple -> Light purple for dark mode
        { "Teal", ("#0F7173", "#4CC2C4") }, // Teal -> Light teal for dark mode
        { "Green", ("#107C10", "#6BCF7F") }, // Green -> Light green for dark mode
        { "Orange", ("#D83B01", "#FF8C00") }, // Orange -> Light orange for dark mode
        { "Red", ("#D13438", "#FF6B6B") }, // Red -> Light red for dark mode
        { "Pink", ("#E3008C", "#FF69B4") }, // Pink -> Light pink for dark mode
        { "Indigo", ("#5C2D91", "#9A7DC4") } // Indigo -> Light indigo for dark mode
    };

    public static string GetAccentColorForTheme(string accentColorName, bool isDarkTheme)
    {
        if (!AccentColors.TryGetValue(accentColorName, out var colorPair))
        {
            // Fallback to Blue if accent color not found
            colorPair = AccentColors["Blue"];
        }

        return isDarkTheme ? colorPair.Dark : colorPair.Light;
    }
}