using LiteDB;
using System;
using System.IO;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.Services
{
    public class LiteDbService : IDisposable
    {
        private LiteDatabase? _database;
        private readonly string _databasePath;

        public LiteDbService()
        {
            try
            {
                _databasePath = GetDatabasePath();
                Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ?? throw new InvalidOperationException()); // Ensure the directory exists
                _database = new LiteDatabase(_databasePath);
                Log.Information("LiteDbService initialized with database path: {DbPath}", _databasePath);

                // Call SeedDatabase to ensure the grids table is generated
                SeedDatabase();
            }
            catch (LiteException ex)
            {
                Log.Error(ex, "LiteDB exception occurred. Attempting to recreate the database.");
                HandleDatabaseCorruption();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize LiteDbService");
                throw;
            }
        }

        private static string GetDatabasePath()
        {
            string appDataPath;
             appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer");
            return Path.Combine(appDataPath, "data.db");
        }

        private void HandleDatabaseCorruption()
        {
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
                _database = new LiteDatabase(_databasePath);
                SeedDatabase();
                Log.Information("Database recreated successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to recreate the database.");
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

        private void SeedDatabase()
        {
            SeedPreferences();
            SeedGrids();
        }

        private void SeedPreferences()
        {
            try
            {
                var preferencesCollection = _database.GetCollection<PreferencesModel>("preferences");
                if (preferencesCollection.Count() != 0) return;
                var defaultPreferences = new PreferencesModel
                {
                    Theme = "Default",
                    LoginLocation = "Home",
                    Font = "Atkinson Hyperlegible",
                    Language = "en-US",
                    SelectedGridNick = "agni",
                    LastSavedEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                preferencesCollection.Insert(defaultPreferences);
                Log.Information("Database seeded with default preferences");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to seed preferences");
            }
        }

        private void SeedGrids()
        {
            try
            {
                var gridsCollection = _database.GetCollection<GridModel>("grids");
                if (gridsCollection.Count() != 0) return;
                var grids = new[]
                {
                    new GridModel
                    {
                        GridNick = "agni",
                        GridName = "Second Life (agni)",
                        Platform = "SecondLife",
                        LoginUri = "https://login.agni.lindenlab.com/cgi-bin/login.cgi",
                        LoginPage = "http://secondlife.com/app/login/?channel=Second+Life+Release",
                        HelperUri = "https://secondlife.com/helpers/",
                        Website = "http://secondlife.com/",
                        Support = "http://secondlife.com/support/",
                        Register = "http://secondlife.com/registration/",
                        Password = "http://secondlife.com/account/request.php",
                        Version = "0"
                    },
                    new GridModel
                    {
                        GridNick = "aditi",
                        GridName = "Second Life Beta (aditi)",
                        Platform = "SecondLife",
                        LoginUri = "https://login.aditi.lindenlab.com/cgi-bin/login.cgi",
                        LoginPage = "http://secondlife.com/app/login/?channel=Second+Life+Beta",
                        HelperUri = "http://aditi-secondlife.webdev.lindenlab.com/helpers/",
                        Website = "http://secondlife.com/",
                        Support = "http://secondlife.com/support/",
                        Register = "http://secondlife.com/registration/",
                        Password = "http://secondlife.com/account/request.php",
                        Version = "1"
                    }
                };

                gridsCollection.InsertBulk(grids);
                Log.Information("Database seeded with default grids");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to seed grids");
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
            Log.Information("LiteDbService disposed");
        }
    }
}