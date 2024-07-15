using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        private PreferencesModel _preferences = new();

        private string _selectedLanguage = "en-US";
        private string _selectedTheme = "Default";
        private string _selectedLoginLocation = "Home";

        public IEnumerable<string> LanguageOptions => _preferences.LanguageOptions;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));
                    _preferences.Language = value;
                    ChangeCulture(value);
                }
            }
        }

        public IEnumerable<string> ThemeOptions => _preferences.ThemeOptions;

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnPropertyChanged(nameof(SelectedTheme));
                    _preferences.Theme = value;
                }
            }
        }

        public IEnumerable<string> LoginLocationOptions => _preferences.LoginLocationOptions;

        public string SelectedLoginLocation
        {
            get => _selectedLoginLocation;
            set
            {
                if (_selectedLoginLocation != value)
                {
                    _selectedLoginLocation = value;
                    OnPropertyChanged(nameof(SelectedLoginLocation));
                    _preferences.LoginLocation = value;
                }
            }
        }

        private static void ChangeCulture(string cultureName)
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        }

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
            _preferencesFilePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GalaxyViewer", "preferences.xml");
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

            // Call InitializeAsync to load preferences and set the application's culture
            Task.Run(InitializeAsync).Wait();
        }

        public async Task InitializeAsync()
        {
            Preferences = await LoadPreferencesAsync();

            _selectedLanguage = Preferences.Language;
            _selectedTheme = Preferences.Theme;
            _selectedLoginLocation = Preferences.LoginLocation;

            // Notify the UI to update
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(SelectedLoginLocation));

            ChangeCulture(
                _selectedLanguage); // Ensure the culture is updated based on the selected language
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                // Step 1: Check and copy default preferences if necessary
                var defaultPreferencesPath = Path.Combine("Assets", "preferences.xml");
                if (!File.Exists(_preferencesFilePath) && File.Exists(defaultPreferencesPath))
                {
                    File.Copy(defaultPreferencesPath, _preferencesFilePath);
                }

                // Step 2: Open the preferences file
                await using var stream = new FileStream(_preferencesFilePath, FileMode.OpenOrCreate,
                    FileAccess.Read, FileShare.Read);
                var serializer = new XmlSerializer(typeof(PreferencesModel));

                // Step 3 & 4: Deserialize the XML content
                var loadedPreferences = await Task.Run(() =>
                {
                    try
                    {
                        var result = serializer.Deserialize(stream);
                        if (result is PreferencesModel preferences)
                        {
                            return preferences;
                        }

                        throw new InvalidCastException(
                            "Deserialized object is not of type PreferencesModel.");
                    }
                    catch (Exception ex)
                    {
                        // Step 5: Handle deserialization failure
                        Log.Error(ex, "Failed to deserialize preferences");
                        StatusMessage = "Failed to load preferences. Using default settings.";
                        return new PreferencesModel(); // Return default preferences on error
                    }
                });
                return loadedPreferences;
            }
            catch (Exception ex)
            {
                // Step 6: Handle any other exceptions
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
                    Directory.CreateDirectory(directoryPath ??
                                              throw new InvalidOperationException(
                                                  "Directory path is null."));
                }

                await using var stream = new FileStream(_preferencesFilePath, FileMode.Create,
                    FileAccess.Write);
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