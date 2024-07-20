using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.Services
{
    public class PreferencesManager
    {
        private readonly string _preferencesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer",
            "preferences.xml");

        private bool _isLoadingPreferences;

        public PreferencesManager()
        {
            EnsurePreferencesDirectory();
        }

        private void EnsurePreferencesDirectory()
        {
            var directoryPath = Path.GetDirectoryName(_preferencesFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public async Task<PreferencesModel> LoadPreferencesAsync()
        {
            try
            {
                _isLoadingPreferences = true;

                if (!File.Exists(_preferencesFilePath))
                {
                    var defaultPreferences = new PreferencesModel();
                    _isLoadingPreferences = false;
                    return defaultPreferences;
                }

                await using var stream =
                    new FileStream(_preferencesFilePath, FileMode.Open, FileAccess.Read);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                var preferences = (PreferencesModel)serializer.Deserialize(stream)!;

                _isLoadingPreferences = false;
                return preferences;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load preferences");
                var defaultPreferences = new PreferencesModel();
                _isLoadingPreferences = false;
                return defaultPreferences;
            }
        }

        public bool IsLoadingPreferences => _isLoadingPreferences;

        public event EventHandler<PreferencesModel>? PreferencesChanged;

        public async Task SavePreferencesAsync(PreferencesModel preferences)
        {
            try
            {
                await using var stream = new FileStream(_preferencesFilePath, FileMode.Create,
                    FileAccess.Write);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                serializer.Serialize(stream, preferences);

                if (!_isLoadingPreferences)
                {
                    PreferencesChanged?.Invoke(this, preferences);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save preferences");
            }
        }
    }
}