using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;
using GalaxyViewer.Views;
using GalaxyViewer.Services;
using Serilog;

namespace GalaxyViewer;

public class App : Application
{
    private readonly PreferencesManager _preferencesManager = new PreferencesManager();

    public App()
    {
        // Initialize Serilog here
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/error.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _preferencesManager.LoadPreferencesAsync();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}