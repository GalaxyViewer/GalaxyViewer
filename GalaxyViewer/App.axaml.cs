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
using Serilog;

namespace GalaxyViewer;

public class App : Application, IDisposable
{
    private static IServiceProvider? _serviceProvider;
    public static PreferencesManager? PreferencesManager { get; private set; }

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
        services.AddSingleton<LiteDbService>();
        // Register other services here
    }

    public override void Initialize()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider();

        var liteDbService = _serviceProvider.GetService<LiteDbService>();
        if (liteDbService == null)
        {
            throw new InvalidOperationException("LiteDbService is not registered.");
        }

        PreferencesManager = new PreferencesManager(liteDbService);
        PreferencesManager.PreferencesChanged += OnPreferencesChanged;

        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    Log.Information("Initializing MainWindow for desktop application.");
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainViewModel()
                    };
                    desktop.MainWindow.Show();
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    Log.Information("Initializing MainView for single view application.");
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = new MainViewModel()
                    };
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the main window.");
            throw; // Optionally rethrow the exception if you want to halt the application
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool _isLoggedIn;

    public static bool IsLoggedIn
    {
        get => _isLoggedIn;
        set
        {
            if (_isLoggedIn != value)
            {
                _isLoggedIn = value;
                OnStaticPropertyChanged();
            }
        }
    }

    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
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