using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.Services;

public class PreferencesManager
{
    private readonly string _preferencesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer", "preferences.xml");
    private bool _preferencesLoaded;

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

    public Task<PreferencesModel> LoadPreferencesAsync()
    {
        if (_preferencesLoaded)
        {
            return Task.FromResult(new PreferencesModel());
        }

        try
        {
            if (!File.Exists(_preferencesFilePath))
            {
                _preferencesLoaded = true;
                return Task.FromResult(new PreferencesModel());
            }

            using var stream = new FileStream(_preferencesFilePath, FileMode.Open, FileAccess.Read);
            var serializer = new XmlSerializer(typeof(PreferencesModel));
            var preferences = (PreferencesModel)serializer.Deserialize(stream)!;
            _preferencesLoaded = true;
            return Task.FromResult(preferences);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load preferences");
            _preferencesLoaded = true;
            return Task.FromResult(new PreferencesModel());
        }
    }

    public async Task SavePreferencesAsync(PreferencesModel preferences)
    {
        try
        {
            await using var stream = new FileStream(_preferencesFilePath, FileMode.Create, FileAccess.Write);
            var serializer = new XmlSerializer(typeof(PreferencesModel));
            serializer.Serialize(stream, preferences);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save preferences");
        }
    }
}