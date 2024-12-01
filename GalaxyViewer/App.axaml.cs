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

namespace GalaxyViewer
{
    public class App : Application, IDisposable
    {
        private static IServiceProvider? _serviceProvider;
        public static PreferencesManager? PreferencesManager { get; private set; }
        public static SessionManager? SessionManager { get; private set; }
        private static ILiteDbService _liteDbService;

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
            services.AddSingleton<ILiteDbService, LiteDbService>();
            services.AddSingleton<SessionManager>();
            services.AddSingleton<IGridService, GridService>();
            services.AddSingleton<PreferencesViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<LoggedInViewModel>();
        }

        public override void Initialize()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            _liteDbService = _serviceProvider.GetRequiredService<ILiteDbService>();
            if (_liteDbService == null)
            {
                throw new InvalidOperationException("LiteDbService is not registered.");
            }

            PreferencesManager = new PreferencesManager(_liteDbService);
            PreferencesManager.PreferencesChanged += OnPreferencesChanged;

            SessionManager = _serviceProvider.GetRequiredService<SessionManager>();
            if (SessionManager == null)
            {
                throw new InvalidOperationException("SessionManager is not registered.");
            }

            SessionManager.SessionChanged += OnSessionChanged;

            AvaloniaXamlLoader.Load(this);
            base.Initialize();
        }

        public static bool IsLoggedIn
        {
            get => SessionManager?.Session.IsLoggedIn ?? false;
            set
            {
                if (SessionManager != null && SessionManager.Session.IsLoggedIn != value)
                {
                    var session = SessionManager.Session;
                    session.IsLoggedIn = value;
                    SessionManager.Session = session;
                    OnStaticPropertyChanged();
                }
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
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

        private void OnSessionChanged(object? sender, SessionModel session)
        {
            if (session.IsLoggedIn)
            {
                IsLoggedIn = true;
            }
            else if (IsLoggedIn)
            {
                IsLoggedIn = false;
            }
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
}