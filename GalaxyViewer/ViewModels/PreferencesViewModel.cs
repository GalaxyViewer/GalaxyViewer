using GalaxyViewer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ReactiveUI;
using System.Reactive;

namespace GalaxyViewer.ViewModels
{
    public class PreferencesViewModel : ReactiveObject
    {
        public IEnumerable<string> ThemeOptions => Enum.GetNames(typeof(ThemeOptions));

        public IEnumerable<string> LoginLocationOptions => Enum.GetNames(typeof(LoginLocationOptions));

        private Lazy<PreferencesModel> _lazyPreferences;
        private readonly string _preferencesFilePath;
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        public PreferencesViewModel()
        {
            _preferencesFilePath = GetPreferencesFilePath();
            _lazyPreferences = new Lazy<PreferencesModel>(LoadOrCreatePreferences);
            SaveCommand = ReactiveCommand.CreateFromTask(SavePreferencesAsync);
        }

        public static async Task<PreferencesViewModel> CreateAsync()
        {
            var viewModel = new PreferencesViewModel();
            await viewModel.LoadPreferencesAsync();
            return viewModel;
        }

        private PreferencesModel Preferences => _lazyPreferences.Value;

        private static string GetPreferencesFilePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var applicationFolder = Path.Combine(appDataFolder, "GalaxyViewer");
            Directory.CreateDirectory(applicationFolder); // CreateDirectory is no-op if exists
            return Path.Combine(applicationFolder, "preferences.xml");
        }

        private PreferencesModel LoadOrCreatePreferences()
        {
            if (File.Exists(_preferencesFilePath))
            {
                using var stream = new FileStream(_preferencesFilePath, FileMode.Open);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                return serializer.Deserialize(stream) as PreferencesModel ?? new PreferencesModel();
            }
            return new PreferencesModel
            {
                Theme = Enum.TryParse(typeof(ThemeOptions), "System", out object themeResult) ? themeResult.ToString() : Models.ThemeOptions.Light.ToString(),
                LoginLocation = Enum.TryParse(typeof(LoginLocationOptions), "LastLocation", out object loginLocationResult) ? loginLocationResult.ToString() : Models.LoginLocationOptions.Home.ToString()
            };
        }

        private string _theme;
        public string Theme
        {
            get => Preferences.Theme;
            set
            {
                if (Preferences.Theme != value)
                {
                    Preferences.Theme = value;
                    this.RaisePropertyChanged(nameof(Theme));
                }
            }
        }

        private string _loginLocation;
        public string LoginLocation
        {
            get => Preferences.LoginLocation;
            set
            {
                if (Preferences.LoginLocation != value)
                {
                    Preferences.LoginLocation = value;
                    this.RaisePropertyChanged(nameof(LoginLocation));
                }
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public async Task LoadPreferencesAsync()
        {
            await Task.Run(() =>
            {
                _lazyPreferences = new Lazy<PreferencesModel>(LoadOrCreatePreferences);
            });
        }

        public async Task SavePreferencesAsync()
        {
            try
            {
                Preferences.LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
                var serializer = new XmlSerializer(typeof(PreferencesModel));

                await Task.Run(() => serializer.Serialize(stream, Preferences));

                StatusMessage = "Preferences saved";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving preferences: {ex.Message}";
            }
        }
    }
}