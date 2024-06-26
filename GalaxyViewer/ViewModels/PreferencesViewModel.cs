using GalaxyViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using ReactiveUI;
using System.Reactive;
using System.ComponentModel;
using Serilog;

namespace GalaxyViewer.ViewModels
{
    public class PreferencesViewModel : ReactiveObject
    {
        public IEnumerable<string> ThemeOptions => Enum.GetNames(typeof(ThemeOptions));
        public IEnumerable<string> LoginLocationOptions => Enum.GetNames(typeof(LoginLocationOptions));

        private PreferencesModel _preferences = new PreferencesModel();
        private readonly string _preferencesFilePath;
        public ReactiveCommand<Unit, Unit>? SaveCommand { get; private set; }

        public PreferencesViewModel()
        {
            _preferencesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer", "preferences.xml");
            var directoryPath = Path.GetDirectoryName(_preferencesFilePath);
            if (directoryPath != null)
            {
                _ = Directory.CreateDirectory(directoryPath);
            }
            else
            {
                Log.Error("Failed to create preferences directory.");
            }
            SaveCommand = ReactiveCommand.CreateFromTask(SavePreferencesAsync);
        }

        public async Task InitializeAsync()
        {
            Preferences = await LoadPreferencesAsync() ?? new PreferencesModel();
        }

        public PreferencesModel Preferences
        {
            get => _preferences;
            set
            {
                this.RaiseAndSetIfChanged(ref _preferences, value);
            }
        }

        public ThemeOptions Theme
        {
            get => Preferences.Theme;
            set
            {
                Preferences.Theme = value;
                this.RaisePropertyChanged(nameof(Theme));
            }
        }

        public LoginLocationOptions LoginLocation
        {
            get => Preferences.LoginLocation;
            set
            {
                Preferences.LoginLocation = value;
                this.RaisePropertyChanged(nameof(LoginLocation));
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public async Task<PreferencesModel> LoadPreferencesAsync()
        {
            try
            {
                string defaultPreferencesPath = Path.Combine("Assets", "preferences.xml");
                if (!File.Exists(_preferencesFilePath) && File.Exists(defaultPreferencesPath))
                {
                    File.Copy(defaultPreferencesPath, _preferencesFilePath);
                }

                using (var stream = new FileStream(_preferencesFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new XmlSerializer(typeof(PreferencesModel));
                    var loadedPreferences = await Task.Run(() =>
                    {
                        var result = serializer.Deserialize(stream);
                        if (result is PreferencesModel preferences)
                        {
                            return preferences;
                        }
                        else
                        {
                            throw new InvalidCastException("Deserialized object is not of type PreferencesModel.");
                        }
                    }) ?? new PreferencesModel();
                    return loadedPreferences ?? new PreferencesModel();
                }
            }
            catch (InvalidCastException ice)
            {
                StatusMessage = $"Invalid cast exception loading preferences: {ice.Message}";
                Log.Error(ice, "Invalid cast while loading preferences.");
                return new PreferencesModel();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading preferences: {ex.Message}";
                Log.Error(ex, "Error loading preferences.");
                return new PreferencesModel();
            }
        }

        public async Task SavePreferencesAsync()
        {
            try
            {
                Preferences.LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var directoryPath = Path.GetDirectoryName(_preferencesFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath ?? throw new InvalidOperationException("Directory path is null."));
                }

                using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                await Task.Run(() => serializer.Serialize(stream, Preferences));
                StatusMessage = "Preferences saved successfully.";
            }
            catch (IOException ioEx)
            {
                StatusMessage = $"Error accessing the preferences file: {ioEx.Message}";
                Log.Error(ioEx, "Error accessing the preferences file.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving preferences: {ex.Message}";
                Log.Error(ex, "Error saving preferences.");
            }
        }
    }
}