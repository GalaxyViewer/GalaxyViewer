using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaxyViewer.Models;
using ReactiveUI;
using Serilog;

namespace GalaxyViewer.ViewModels
{
    public sealed class PreferencesViewModel : INotifyPropertyChanged
    {
        public IEnumerable<string> ThemeOptions => Enum.GetNames(typeof(ThemeOptions));
        public IEnumerable<string> LoginLocationOptions => Enum.GetNames(typeof(LoginLocationOptions));

        private PreferencesModel _preferences = new PreferencesModel();

        public PreferencesModel Preferences
        {
            get => _preferences;
            set
            {
                _preferences = value;
                OnPropertyChanged(nameof(Preferences));
            }
        }
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
                Log.Error("Failed to create preferences directory");
            }

            SaveCommand = ReactiveCommand.CreateFromTask(SavePreferencesAsync);
        }

/*
        public async Task InitializeAsync()
        {
            Preferences = await LoadPreferencesAsync() ?? new PreferencesModel();
        }
*/

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ThemeOptions _selectedTheme;
        public ThemeOptions SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                _selectedTheme = value;
                OnPropertyChanged(nameof(SelectedTheme));
            }
        }

        private LoginLocationOptions _selectedLoginLocation;
        public LoginLocationOptions SelectedLoginLocation
        {
            get => _selectedLoginLocation;
            set
            {
                _selectedLoginLocation = value;
                OnPropertyChanged(nameof(SelectedLoginLocation));
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public async Task<PreferencesModel> LoadPreferencesAsync()
        {
            try
            {
                var defaultPreferencesPath = Path.Combine("Assets", "preferences.xml");
                if (!File.Exists(_preferencesFilePath) && File.Exists(defaultPreferencesPath))
                {
                    File.Copy(defaultPreferencesPath, _preferencesFilePath);
                }

                await using var stream = new FileStream(_preferencesFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                var loadedPreferences = await Task.Run(() =>
                {
                    try
                    {
                        var result = serializer.Deserialize(stream);
                        if (result is PreferencesModel preferences)
                        {
                            return preferences;
                        }

                        throw new InvalidCastException("Deserialized object is not of type PreferencesModel.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to deserialize preferences");
                        StatusMessage = "Failed to load preferences. Using default settings.";
                        return new PreferencesModel(); // Return default preferences on error
                    }
                });
                return loadedPreferences;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading preferences");
                StatusMessage = "Error loading preferences. Using default settings.";
                return new PreferencesModel(); // Return default preferences on error
            }
        }

        private async Task SavePreferencesAsync()
        {
            try
            {
                Preferences.LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var directoryPath = Path.GetDirectoryName(_preferencesFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath ?? throw new InvalidOperationException("Directory path is null."));
                }

                await using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                await Task.Run(() => serializer.Serialize(stream, Preferences));
                StatusMessage = "Preferences saved successfully.";
            }
            catch (IOException ioEx)
            {
                StatusMessage = $"Error accessing the preferences file: {ioEx.Message}";
                Log.Error(ioEx, "Error accessing the preferences file");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving preferences: {ex.Message}";
                Log.Error(ex, "Error saving preferences");
            }
        }
    }
}