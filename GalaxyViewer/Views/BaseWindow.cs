using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using GalaxyViewer.Models;

namespace GalaxyViewer.Views
{
    public class BaseWindow : Window
    {
        protected BaseWindow()
        {
            Icon = new WindowIcon("Assets/galaxy.ico");
            CanResize = true;
            App.PreferencesManager!.PreferencesChanged += OnPreferencesChanged;
            var preferences = App.PreferencesManager.LoadPreferencesAsync().Result;
            ApplyTheme(preferences.Theme);
            FontFamily = new FontFamily(preferences.Font);
        }

        private void OnPreferencesChanged(object? sender, PreferencesModel preferences)
        {
            ApplyTheme(preferences.Theme);
            FontFamily = new FontFamily(preferences.Font); // Update the font family
        }

        public void ApplyTheme(string theme)
        {
            RequestedThemeVariant = theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }
    }
}