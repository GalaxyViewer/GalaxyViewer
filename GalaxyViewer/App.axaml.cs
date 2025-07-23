using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Platform;
using Avalonia.Media;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using GalaxyViewer.ViewModels;
using GalaxyViewer.Views;
using Microsoft.Extensions.DependencyInjection;
using OpenMetaverse;
using Serilog;

namespace GalaxyViewer;

public class App : Application, IDisposable
{
    private static IServiceProvider? _serviceProvider;
    public static PreferencesManager? PreferencesManager { get; private set; }
    private static LiteDbService? _liteDbService;
    private static GridClient? _gridClient;

    private static bool _isLoggedIn;
    private IPlatformSettings? _platformSettings;

    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    public static bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn == value) return;
            _isLoggedIn = value;
            OnStaticPropertyChanged();
        }
    }

    public App()
    {
        ConfigureLogging();
    }

    private static void ConfigureLogging()
    {
        var logFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GalaxyViewer", "logs", "error.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] GALAXYVIEWER: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        _gridClient = new GridClient();
        services.AddSingleton(_gridClient);
        services.AddSingleton<LiteDbService>(_ => new LiteDbService(_gridClient));
        services.AddSingleton<SessionService>(provider =>
            new SessionService(
                provider.GetRequiredService<LiteDbService>(),
                provider.GetRequiredService<GridClient>()
            ));
    }

    public override void Initialize()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _liteDbService = _serviceProvider.GetRequiredService<LiteDbService>();

        PreferencesManager = new PreferencesManager(_liteDbService);

        PreferencesManager.PreferencesChanged += OnPreferencesChanged;

        AvaloniaXamlLoader.Load(this);

        try
        {
            _platformSettings = PlatformSettings;
            if (_platformSettings != null)
            {
                _platformSettings.ColorValuesChanged += OnSystemThemeChanged;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to subscribe to system theme changes");
        }

        base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (_serviceProvider == null)
            {
                Log.Error("Service provider is null during framework initialization");
                throw new InvalidOperationException(
                    "Service provider was not properly initialized");
            }

            var liteDbService = _serviceProvider.GetRequiredService<LiteDbService>();
            var gridClient = _serviceProvider.GetRequiredService<GridClient>();
            var sessionService = _serviceProvider.GetRequiredService<SessionService>();

            Log.Information("Services initialized successfully");

            // Create UI first to avoid blocking
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainViewModel(liteDbService, gridClient, sessionService)
                    };
                    desktop.MainWindow.Show();
                    Log.Information("Desktop main window created and shown");
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    Log.Information("Creating MainView for single view platform (Android)");
                    break;
                default:
                    Log.Warning("Unknown application lifetime type: {Type}",
                        ApplicationLifetime?.GetType().Name);
                    break;
            }

            Task.Run(async () =>
            {
                try
                {
                    if (PreferencesManager != null)
                    {
                        var preferences = await PreferencesManager.LoadPreferencesAsync();
                        if (preferences != null)
                        {
                            ApplyPreferences(preferences);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to load initial preferences");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the main window.");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }


    private static void OnStaticPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
    }

    private void OnSystemThemeChanged(object? sender, PlatformColorValues e)
    {
        Task.Run(async () =>
        {
            try
            {
                if (PreferencesManager != null)
                {
                    var preferences = await PreferencesManager.LoadPreferencesAsync();
                    if (preferences?.Theme == "System")
                    {
                        ApplyPreferences(preferences);
                        RefreshThemeForAllWindows();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error handling system theme change");
            }
        });
    }

    private void OnPreferencesChanged(object? sender, PreferencesModel preferences)
    {
        ApplyPreferences(preferences);
        RefreshThemeForAllWindows();
    }

    private void ApplyPreferences(PreferencesModel preferences)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            RequestedThemeVariant = GetThemeVariant(preferences.Theme);

            var isDarkTheme = RequestedThemeVariant == ThemeVariant.Dark ||
                              (RequestedThemeVariant == ThemeVariant.Default &&
                               DetectSystemTheme() == ThemeVariant.Dark);

            UpdateAccentColorResource(preferences.AccentColor);
            UpdateTextColorResource(isDarkTheme);
        });
    }

    private void UpdateAccentColorResource(string accentColorPreference)
    {
        try
        {
            IBrush accentBrush;

            if (accentColorPreference == "System Default")
            {
                accentBrush = GetSystemAccentBrush();
            }
            else
            {
                var isDarkTheme = RequestedThemeVariant == ThemeVariant.Dark ||
                                  (RequestedThemeVariant == ThemeVariant.Default &&
                                   DetectSystemTheme() == ThemeVariant.Dark);

                var colorValue =
                    PreferencesOptions.GetAccentColorForTheme(accentColorPreference, isDarkTheme);

                try
                {
                    accentBrush = new SolidColorBrush(Color.Parse(colorValue));
                }
                catch
                {
                    accentBrush = GetSystemAccentBrush();
                }
            }

            Resources["SystemAccentColorBrush"] = accentBrush;
            Resources["SystemAccentColor"] = ((SolidColorBrush)accentBrush).Color;
            // Ensure these resources are available for DynamicResource lookups
            if (Current == null) return;
            Current.Resources["SystemAccentColorBrush"] = accentBrush;
            Current.Resources["SystemAccentColor"] = ((SolidColorBrush)accentBrush).Color;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to update accent color resource");
        }
    }

    internal static IBrush GetSystemAccentBrush()
    {
        try
        {
            if (Current?.TryGetResource("SystemAccentColorBrush",
                    Current.ActualThemeVariant, out var resource) == true)
            {
                if (resource is IBrush brush)
                    return brush;
            }

            if (Current?.TryGetResource("SystemAccentColor",
                    Current.ActualThemeVariant, out var colorResource) == true)
            {
                if (colorResource is Color color)
                    return new SolidColorBrush(color);
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Final fallback to a reasonable blue
        return new SolidColorBrush(Color.Parse("#0078D4"));
    }

    private void UpdateTextColorResource(bool isDarkTheme)
    {
        var color = isDarkTheme ? Color.Parse("#F0F0F0") : Color.Parse("#222222");
        var brush = new SolidColorBrush(color);

        Resources["TextColor"] = color;
        Resources["TextColorBrush"] = brush;
        if (Current != null)
        {
            Current.Resources["TextColor"] = color;
            Current.Resources["TextColorBrush"] = brush;
        }
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
            if (_platformSettings != null)
            {
                var colorValues = _platformSettings.GetColorValues();
                return colorValues.ThemeVariant == PlatformThemeVariant.Dark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to detect system theme, falling back to default");
        }

        return ThemeVariant.Default;
    }

    private async void RefreshThemeForAllWindows()
    {
        try
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
                return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    if (PreferencesManager != null)
                    {
                        var preferences = await PreferencesManager.LoadPreferencesAsync();
                        if (preferences?.Theme != null)
                        {
                            foreach (var window in desktopLifetime.Windows)
                            {
                                if (window is BaseWindow baseWindow)
                                {
                                    baseWindow.ApplyTheme(preferences.Theme);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error refreshing themes for windows");
                }
            });
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to refresh theme for all windows");
        }
    }

    // TODO: Implement a method to set language resources based on user preferences
    // Currently we only have US English resources
    private static void SetLanguageResources()
    {
        var language = PreferencesManager?.CurrentPreferences?.Language ?? "en-US";
        var resources = Current?.Resources;
        if (resources == null) return;
        resources.MergedDictionaries.Clear();

        if (language == "en-US")
        {
            resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri("avares://GalaxyViewer/Resources/Strings.axaml")));
        }
    }

    public void Dispose()
    {
        if (_platformSettings != null)
        {
            _platformSettings.ColorValuesChanged -= OnSystemThemeChanged;
        }

        //PreferencesManager?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}