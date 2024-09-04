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
            ApplyTheme(App.PreferencesManager.LoadPreferencesAsync().Result.Theme);
            FontFamily = new FontFamily("Atkinson Hyperlegible"); // TODO: Make this use the preferences for font
        }

        private void OnPreferencesChanged(object? sender, PreferencesModel preferences)
        {
            ApplyTheme(preferences.Theme);
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