using System;
using System.IO;
using GalaxyViewer.Models;
using LiteDB;
using Serilog;

namespace GalaxyViewer.Services
{
    public class LiteDbService : ILiteDbService, IDisposable
    {
        private readonly LiteDatabase _database;
        private readonly string _databasePath;

        public LiteDbService()
        {
            _databasePath = GetDatabasePath();
            _database = new LiteDatabase(_databasePath);
            Log.Information("LiteDbService initialized with database path: {DbPath}", _databasePath);
            SeedDatabase();
        }

        public LiteDatabase Database => _database;

        private static string GetDatabasePath()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer");
            return Path.Combine(appDataPath, "data.db");
        }

        private void SeedDatabase()
        {
            SeedPreferences();
            SeedGrids();
        }

        private void SeedPreferences()
        {
            var preferencesCollection = _database.GetCollection<PreferencesModel>("preferences");
            if (preferencesCollection.Count() == 0)
            {
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
        }

        private void SeedGrids()
        {
            var gridsCollection = _database.GetCollection<GridModel>("grids");
            if (gridsCollection.Count() == 0)
            {
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
        }

        private void ClearSessionData()
        {
            // Commented out the code that clears the session data
            // ILiteCollection<SessionModel>? sessionCollection;
            // if (_database.CollectionExists("session"))
            // {
            //     sessionCollection = _database.GetCollection<SessionModel>("session");
            //     sessionCollection.DeleteAll();
            //     Log.Information("Session data cleared on startup");
            // }

            var sessionCollection = _database.GetCollection<SessionModel>("session");
            if (sessionCollection.Count() == 0)
            {
                sessionCollection.Insert(new SessionModel());
                Log.Information("Session data created on startup");
            }
        }

        public ILiteCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        public SessionModel GetSession()
        {
            return _database.GetCollection<SessionModel>("sessions").FindOne(Query.All()) ?? new SessionModel();
        }

        public void SaveSession(SessionModel session)
        {
            _database.GetCollection<SessionModel>("sessions").Upsert(session);
        }

        public void Dispose()
        {
            _database?.Dispose();
            Log.Information("LiteDbService disposed");
        }
    }
}