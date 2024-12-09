using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiteDB;
using GalaxyViewer.Models;
using System.Threading.Tasks;

namespace GalaxyViewer.Services;

public sealed class PreferencesManager
{
    private readonly ILiteCollection<PreferencesModel> _preferencesCollection;
    private readonly ILiteCollection<GridModel>? _gridsCollection;

    public event EventHandler<PreferencesModel>? PreferencesChanged;

    public PreferencesManager(LiteDbService? liteDbService)
    {
        Debug.Assert(liteDbService != null, nameof(liteDbService) + " != null");
        var database = liteDbService?.Database;
        Debug.Assert(database != null, nameof(database) + " != null");
        _preferencesCollection = database.GetCollection<PreferencesModel>("preferences");
        _gridsCollection = database.GetCollection<GridModel>("grids");
    }

    public PreferencesModel CurrentPreferences
    {
        get
        {
            var preferences = _preferencesCollection.FindOne(Query.All());
            return preferences ?? new PreferencesModel
            {
                Id = ObjectId.NewObjectId(),
                Theme = "Default",
                LoginLocation = "Home",
                Font = "Atkinson Hyperlegible",
                Language = "en-US",
                LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SelectedGridNick = string.Empty
            };
        }
    }

    public async Task<PreferencesModel> LoadPreferencesAsync()
    {
        return await Task.Run(() =>
        {
            var preferences = _preferencesCollection.FindOne(Query.All());
            return preferences ?? new PreferencesModel
            {
                Id = ObjectId.NewObjectId(),
                Theme = "Default",
                LoginLocation = "Home",
                Font = "Atkinson Hyperlegible",
                Language = "en-US",
                SelectedGridNick = "agni",
                LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
        });
    }

    public async Task SavePreferencesAsync(PreferencesModel preferences)
    {
        await Task.Run(() =>
        {
            _preferencesCollection.Upsert(preferences);
            OnPreferencesChanged(preferences);
        });
    }

    public Dictionary<string, List<string>> GetCurrentPreferencesOptions()
    {
        return new Dictionary<string, List<string>>
        {
            { "ThemeOptions", PreferencesOptions.ThemeOptions },
            { "LoginLocationOptions", PreferencesOptions.LoginLocationOptions },
            { "FontOptions", PreferencesOptions.FontOptions },
            { "LanguageOptions", PreferencesOptions.LanguageOptions }
        };
    }

    public List<string> GetGridOptions()
    {
        return _gridsCollection?.FindAll().Select(grid => grid.GridNick).ToList() ??
               [];
    }

    private void OnPreferencesChanged(PreferencesModel preferences)
    {
        PreferencesChanged?.Invoke(this, preferences);
    }
}