using LiteDB;
using System;
using System.IO;
using System.Threading.Tasks;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.Services
{
    public class LiteDbService : IDisposable
    {
        private readonly LiteDatabase? _database;

        public LiteDbService()
        {
            try
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer", "data.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath) ?? throw new InvalidOperationException()); // Ensure the directory exists
                _database = new LiteDatabase(dbPath);
                Log.Information("LiteDbService initialized with database path: {DbPath}", dbPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize LiteDbService");
                throw;
            }
        }

        public ILiteCollection<T> GetCollection<T>(string name)
        {
            Log.Information("Retrieving collection: {CollectionName}", name);
            return _database.GetCollection<T>(name);
        }

        public LiteDatabase? Database()
        {
            return _database;
        }

        public void SeedDatabase()
        {
            try
            {
                var preferencesCollection = _database.GetCollection<PreferencesModel>("preferences");
                var count = preferencesCollection.Count();
                Log.Information("Number of records in preferences collection: {Count}", count);

                if (count == 0)
                {
                    var defaultPreferences = new PreferencesModel
                    {
                        Theme = "Default",
                        LoginLocation = "Home",
                        Font = "Atkinson Hyperlegible",
                        Language = "en-US",
                        LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    preferencesCollection.Insert(defaultPreferences);
                    Log.Information("Database seeded with default preferences");
                }

                count = preferencesCollection.Count();
                Log.Information("Number of records in preferences collection: {Count}", count);

                var allPreferences = preferencesCollection.FindAll();
                foreach (var preference in allPreferences)
                {
                    Log.Information("Preference: {@Preference}", preference);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to seed database");
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
            Log.Information("LiteDbService disposed");
        }
    }
}