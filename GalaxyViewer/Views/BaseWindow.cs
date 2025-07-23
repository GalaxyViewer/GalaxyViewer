using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Platform;
using Avalonia.Input;
using GalaxyViewer.Models;
using Serilog;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views;

public class BaseWindow : Window
{
    protected BaseWindow()
    {
        Title = "GalaxyViewer";
        Icon = new WindowIcon("Assets/GalaxyViewerLogo.ico");
        CanResize = true;

        // Use only native window decorations - no custom chrome
        SystemDecorations = SystemDecorations.Full;
        ExtendClientAreaToDecorationsHint = false;

        if (App.PreferencesManager != null)
            App.PreferencesManager.PreferencesChanged += OnPreferencesChanged;

        _ = LoadPreferencesAsync();
    }


    private async Task LoadPreferencesAsync()
    {
        if (App.PreferencesManager == null) return;
        var preferences = await App.PreferencesManager.LoadPreferencesAsync();
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyTheme(preferences.Theme);
            FontFamily = new FontFamily(preferences.Font);
        });
    }

    private void OnPreferencesChanged(object? sender, PreferencesModel preferences)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyTheme(preferences.Theme);
            FontFamily = new FontFamily(preferences.Font);
        });
    }

    internal void ApplyTheme(string theme)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            RequestedThemeVariant = GetThemeVariant(theme);
        });
    }

    private ThemeVariant GetThemeVariant(string themePreference)
    {
        return themePreference switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            "System" => DetectSystemTheme(),
            _ => ThemeVariant.Default
        };
    }

    private ThemeVariant DetectSystemTheme()
    {
        try
        {
            var platformSettings = PlatformSettings;
            if (platformSettings != null)
            {
                var colorValues = platformSettings.GetColorValues();
                return colorValues.ThemeVariant == PlatformThemeVariant.Dark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to detect system theme in BaseWindow, falling back to default");
        }

        return ThemeVariant.Default;
    }
}