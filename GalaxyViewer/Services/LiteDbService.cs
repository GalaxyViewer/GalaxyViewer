using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Markup.Xaml.MarkupExtensions;
using LiteDB;
using GalaxyViewer.Models;
using Serilog;

namespace GalaxyViewer.Services;

public class LiteDbService : IDisposable, INotifyPropertyChanged
{
    private LiteDatabase? _database;
    private readonly string _databasePath;
    public LiteDatabase? Database => _database;

    private SessionModel _session;
    public SessionModel Session
    {
        get => _session;
        private set
        {
            _session = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public LiteDbService()
    {
        _databasePath = GetDatabasePath();
        InitializeDatabase();
        Session = GetSession();
    }

    private static string GetDatabasePath()
    {
        var appDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GalaxyViewer");
        return Path.Combine(appDataPath, "data.db");
    }

    private void InitializeDatabase()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ??
                                      throw new InvalidOperationException());
            _database = new LiteDatabase(_databasePath);
            Log.Information("LiteDbService initialized with database path: {DbPath}",
                _databasePath);
            ClearSessionData();
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

    private void HandleDatabaseCorruption()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                File.Move(_databasePath, _databasePath + ".bak");
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
        ILiteCollection<SessionModel>? sessionCollection;
        if (_database.CollectionExists("session"))
        {
            sessionCollection = _database.GetCollection<SessionModel>("session");
            sessionCollection.DeleteAll();
            Log.Information("Session data cleared on startup");
        }

        sessionCollection = _database.GetCollection<SessionModel>("session");
        sessionCollection.Insert(new SessionModel());
        Log.Information("Session data created on startup");
    }

    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SessionModel GetSession()
    {
        var collection = _database.GetCollection<SessionModel>("session");
        return collection.FindOne(Query.All()) ?? new SessionModel();
    }

    public bool HasSessionChanged(SessionModel currentSession)
    {
        var storedSession = GetSession();
        return !storedSession.Equals(currentSession);
    }

    public void SaveSession(SessionModel session)
    {
        var collection = _database.GetCollection<SessionModel>("session");
        collection.Upsert(session);
        Log.Information("Session data saved");

        Session = session;
    }

    public void Dispose()
    {
        _database?.Dispose();
        Log.Information("LiteDbService disposed");
    }
}