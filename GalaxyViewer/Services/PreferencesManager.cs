using System;
using System.Diagnostics;
using LiteDB;
using GalaxyViewer.Models;
using System.Threading.Tasks;

namespace GalaxyViewer.Services
{
    public sealed class PreferencesManager
    {
        private readonly ILiteCollection<PreferencesModel> _preferencesCollection;

        public event EventHandler<PreferencesModel>? PreferencesChanged;

        public PreferencesManager(LiteDbService? liteDbService)
        {
            Debug.Assert(liteDbService != null, nameof(liteDbService) + " != null");
            var database = liteDbService.Database();
            Debug.Assert(database != null, nameof(database) + " != null");
            _preferencesCollection = database.GetCollection<PreferencesModel>("preferences");
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
                    LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
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

        private void OnPreferencesChanged(PreferencesModel preferences)
        {
            PreferencesChanged?.Invoke(this, preferences);
        }
    }
}