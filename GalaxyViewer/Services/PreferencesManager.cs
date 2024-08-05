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

        public PreferencesModel CurrentPreferences { get; private set; }

        public PreferencesManager(PreferencesModel currentPreferences)
        {
            CurrentPreferences = currentPreferences;
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
                CurrentPreferences = (PreferencesModel)serializer.Deserialize(stream)!;
                IsLoadingPreferences = false;
                return CurrentPreferences;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load preferences");
                CurrentPreferences = new PreferencesModel();
                IsLoadingPreferences = false;
                return CurrentPreferences;
            }
        }

        public bool IsLoadingPreferences { get; private set; }

        public event EventHandler<PreferencesModel>? PreferencesChanged;

        public async Task SavePreferencesAsync(PreferencesModel preferences)
        {
            try
            {
                await using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                var serializer = new XmlSerializer(typeof(PreferencesModel));
                serializer.Serialize(stream, preferences);
                CurrentPreferences = preferences;
                if (!IsLoadingPreferences)
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