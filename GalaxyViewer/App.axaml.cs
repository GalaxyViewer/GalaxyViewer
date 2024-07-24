using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GalaxyViewer.Models;
using GalaxyViewer.ViewModels;
using GalaxyViewer.Views;
using GalaxyViewer.Services;
using Serilog;

namespace GalaxyViewer;

public class App : Application
{
    public App()
    {
        // Initialize Serilog here
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/error.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static PreferencesManager? PreferencesManager { get; private set; }

    public override void Initialize()
    {
        PreferencesManager = new PreferencesManager();
        PreferencesManager.PreferencesChanged += OnPreferencesChanged;
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var contentControl = new ContentControl();
        var navigationService = new NavigationService(contentControl);

        // Register routes
        navigationService.RegisterRoute("login", typeof(LoginView));
        navigationService.RegisterRoute("debug", typeof(DebugView));
        navigationService.RegisterRoute("preferences", typeof(PreferencesView));

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel(navigationService)
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel(navigationService)
                };
                break;
        }

        Debug.Assert(PreferencesManager != null, nameof(PreferencesManager) + " != null");
        var preferences = await PreferencesManager.LoadPreferencesAsync();
        ApplyPreferences(preferences);

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnPreferencesChanged(object? sender, PreferencesModel preferences)
    {
        if (PreferencesManager?.IsLoadingPreferences == true) return;
        ApplyPreferences(preferences);
        RefreshThemeForAllWindows();
    }

    private void ApplyPreferences(PreferencesModel preferences)
    {
        RequestedThemeVariant = preferences.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        // TODO: Apply other preferences
    }

    private void RefreshThemeForAllWindows()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            return;
        foreach (var window in desktopLifetime.Windows)
        {
            if (window is not BaseWindow baseWindow) continue;
            var resultTheme = PreferencesManager?.LoadPreferencesAsync().Result.Theme;
            if (resultTheme != null)
                baseWindow.ApplyTheme(resultTheme);
        }
    }
}