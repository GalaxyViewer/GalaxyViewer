using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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

    public static bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn == value) return;
            _isLoggedIn = value;
            OnStaticPropertyChanged(nameof(IsLoggedIn));
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
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        _gridClient = new GridClient();
        services.AddSingleton(_gridClient);
        services.AddSingleton<LiteDbService>(provider => new LiteDbService(_gridClient));
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
        base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            var liteDbService = _serviceProvider.GetRequiredService<LiteDbService>();
            var gridClient = _serviceProvider.GetRequiredService<GridClient>();
            var sessionService = _serviceProvider.GetRequiredService<SessionService>();

            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainViewModel(liteDbService, gridClient, sessionService)
                    };
                    desktop.MainWindow.Show();
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = new MainViewModel(liteDbService, gridClient, sessionService)
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the main window.");
            throw;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    internal static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
    {
        StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
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
            RequestedThemeVariant = preferences.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            // TODO: Apply other preferences
        });
    }

    private async void RefreshThemeForAllWindows()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktopLifetime)
            return;

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            foreach (var window in desktopLifetime.Windows)
            {
                if (window is not BaseWindow baseWindow) continue;
                var resultTheme = (await PreferencesManager?.LoadPreferencesAsync())?.Theme;
                if (resultTheme != null)
                    baseWindow.ApplyTheme(resultTheme);
            }
        });
    }

    public void Dispose()
    {
        //PreferencesManager?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}