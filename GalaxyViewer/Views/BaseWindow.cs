using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using GalaxyViewer.Models;
using Ursa.ReactiveUIExtension;

namespace GalaxyViewer.Views;

public class BaseWindow : ReactiveUrsaWindow<PreferencesModel>, IStyleable
{
    Type IStyleable.StyleKey => typeof(Window);

    protected BaseWindow()
    {
        Title = "GalaxyViewer";
        Icon = new WindowIcon("Assets/GalaxyViewerLogo.ico");
        CanResize = true;
        if (App.PreferencesManager != null)
            App.PreferencesManager.PreferencesChanged += OnPreferencesChanged;

        // Load preferences asynchronously without blocking the UI thread
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
            RequestedThemeVariant = theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        });
    }
}