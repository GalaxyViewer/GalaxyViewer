using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using GalaxyViewer.Models;
using System.Threading.Tasks;
using Serilog;

namespace GalaxyViewer.Services;

public sealed class PreferencesManager
{
    private readonly LiteDbService _liteDbService;
    private readonly ILiteCollection<PreferencesModel> _preferencesCollection;
    private readonly ILiteCollection<GridModel>? _gridsCollection;
    private PreferencesModel _currentPreferences;

    public event EventHandler<PreferencesModel>? PreferencesChanged;

    public PreferencesManager(LiteDbService? liteDbService)
    {
        _liteDbService = liteDbService ?? throw new ArgumentNullException(nameof(liteDbService));
        var database = _liteDbService.Database ?? throw new InvalidOperationException("Database not initialized");
        _preferencesCollection = database.GetCollection<PreferencesModel>("preferences");
        _gridsCollection = database.GetCollection<GridModel>("grids");
        _currentPreferences = LoadRawPreferences();
        if (!string.IsNullOrEmpty(_currentPreferences.Version) &&
            _currentPreferences.Version.Contains('.')) return;
        _currentPreferences.Version = VersionHelper.GetInformationalVersion();
        _currentPreferences.LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _preferencesCollection.Upsert(_currentPreferences);
    }

    public PreferencesModel CurrentPreferences => _currentPreferences;

    public Task<PreferencesModel> LoadPreferencesAsync() => Task.FromResult(_currentPreferences);

    private PreferencesModel LoadRawPreferences()
    {
        var rawColumn = _liteDbService.Database?.GetCollection<BsonDocument>("preferences");
        var column = rawColumn?.FindOne(Query.All());
        if (column == null)
        {
            return CreateDefaultPreferences();
        }
        var preferences = new PreferencesModel
        {
            Id = column["_id"].AsObjectId,
            Theme = column.TryGetValue("Theme", out var value) ? value.AsString : "System",
            LoginLocation = column.TryGetValue("LoginLocation", out var value1) ? value1.AsString : "Home",
            Font = column.TryGetValue("Font", out var value2) ? value2.AsString : "Atkinson Hyperlegible",
            Language = column.TryGetValue("Language", out var value3) ? value3.AsString : "en-US",
            SelectedGridNick = column.TryGetValue("SelectedGridNick", out var value4) ? value4.AsString : "",
            AccentColor = column.TryGetValue("AccentColor", out var value5) ? value5.AsString : "System Default",
            LastSavedEpoch = column.TryGetValue("LastSavedEpoch", out var value6) ? value6.AsInt64 : 0,
        };
        if (column.TryGetValue("Version", out var v))
        {
            preferences.Version = v.IsString ? v.AsString : v.RawValue?.ToString() ?? string.Empty;
        }
        else
        {
            preferences.Version = string.Empty;
        }
        return preferences;
    }

    public async Task SavePreferencesAsync(PreferencesModel preferences)
    {
        await Task.Run(() =>
        {
            preferences.LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            preferences.Version = VersionHelper.GetInformationalVersion();
            _preferencesCollection.DeleteAll();
            _preferencesCollection.Insert(preferences);
            _liteDbService.Database?.Checkpoint();
            _currentPreferences = preferences;
            PreferencesChanged?.Invoke(this, preferences);
        });
    }

    public Dictionary<string, List<string>> GetCurrentPreferencesOptions()
    {
        return new Dictionary<string, List<string>>
        {
            { "ThemeOptions", PreferencesOptions.ThemeOptions },
            { "LoginLocationOptions", PreferencesOptions.LoginLocationOptions },
            { "FontOptions", PreferencesOptions.FontOptions },
            { "LanguageOptions", PreferencesOptions.LanguageOptions },
            { "AccentColorOptions", PreferencesOptions.AccentColorOptions }
        };
    }

    public List<string> GetGridOptions()
    {
        return _gridsCollection?.FindAll().Select(grid => grid.GridNick).ToList() ?? new List<string>();
    }

    public static PreferencesModel CreateDefaultPreferences()
    {
        return new PreferencesModel
        {
            Id = ObjectId.NewObjectId(),
            Theme = "System",
            LoginLocation = "Home",
            Font = "Atkinson Hyperlegible",
            Language = "en-US",
            AccentColor = "System Default",
            SelectedGridNick = "agni",
            LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Version = VersionHelper.GetInformationalVersion()
        };
    }
}