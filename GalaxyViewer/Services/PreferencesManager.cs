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

        private PreferencesModel _currentPreferences;
        public PreferencesModel CurrentPreferences => _currentPreferences;

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
                await using var stream = new FileStream(_preferencesFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                _currentPreferences = (PreferencesModel)serializer.Deserialize(stream)!;
                _isLoadingPreferences = false;
                return _currentPreferences;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load preferences");
                _currentPreferences = new PreferencesModel();
                _isLoadingPreferences = false;
                return _currentPreferences;
            }
        }

        public bool IsLoadingPreferences => _isLoadingPreferences;

        public event EventHandler<PreferencesModel>? PreferencesChanged;

        public async Task SavePreferencesAsync(PreferencesModel preferences)
        {
            try
            {
                await using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                serializer.Serialize(stream, preferences);
                _currentPreferences = preferences;
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