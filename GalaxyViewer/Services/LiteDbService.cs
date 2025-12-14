using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using LiteDB;
using GalaxyViewer.Models;
using OpenMetaverse;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace GalaxyViewer.Services;

public class LiteDbService : IDisposable, INotifyPropertyChanged
{
    private LiteDatabase? _database;
    private readonly string _databasePath;
    public LiteDatabase? Database { get { lock (_dbLock) { return _database; } } }
    private readonly GridClient _client;

    public LiteDbService(GridClient client)
    {
        _client = client;
        _databasePath = GetDatabasePath();
        InitializeDatabase();
        Session = GetSession();
    }

    private SessionModel _session;

    public SessionModel Session
    {
        get => _session;
        set
        {
            _session = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string GetDatabasePath()
    {
        var appDataPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GalaxyViewer");
        return Path.Combine(appDataPath, "data.db");
    }

    private readonly object _dbLock = new();

    private void InitializeDatabase()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_databasePath) ??
                                      throw new InvalidOperationException());
            lock (_dbLock)
            {
                _database = new LiteDatabase(_databasePath);
            }
            Log.Information("LiteDbService initialized with database path: {DbPath}", _databasePath);

            var gridsCollection = _database.GetCollection<GridModel>("grids");
            if (gridsCollection.Count() == 0)
            {
                SeedGrids();
            }

            ClearSessionData();
            SeedDatabase();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize LiteDbService");
            throw;
        }
    }


    private void SeedDatabase()
    {
        ClearSessionData();
        SeedPreferences();
    }

    private void SeedPreferences()
    {
        lock (_dbLock)
        {
            var preferencesCollection = _database?.GetCollection<PreferencesModel>("preferences");
            if (preferencesCollection != null && preferencesCollection.Count() != 0) return;
            var defaultPreferences = PreferencesManager.CreateDefaultPreferences();
            preferencesCollection?.Insert(defaultPreferences);
        }
        // Log.Debug("Database seeded with default preferences");
    }

    private void SeedGrids()
    {
        lock (_dbLock)
        {
            var gridsCollection = _database?.GetCollection<GridModel>("grids");
            if (gridsCollection != null && gridsCollection.Count() != 0) return;
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
            gridsCollection?.InsertBulk(grids);
        }
        // Log.Debug("Database seeded with default grids");
    }

    private void ClearSessionData()
    {
        lock (_dbLock)
        {
            var sessionCollection = _database?.GetCollection<SessionModel>("session");
            if (sessionCollection != null && sessionCollection.Count() > 0)
            {
                sessionCollection.DeleteAll();
                // Log.Debug("Session data cleared");
            }

            if (sessionCollection != null && sessionCollection.Count() != 0) return;
            sessionCollection?.Insert(new SessionModel());
            // Log.Debug("Session data created");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event EventHandler? SessionChanged;

    public SessionModel GetSession()
    {
        lock (_dbLock)
        {
            var collection = _database?.GetCollection<SessionModel>("session");
            return collection?.FindOne(Query.All()) ?? new SessionModel();
        }
    }

    public async Task<T?> ExecuteDbAsync<T>(Func<LiteDatabase, T> func, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_dbLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _database != null ? func(_database) : default;
            }
        }, cancellationToken);
    }

    public async Task<SessionModel> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<SessionModel>("session");
            return collection.FindOne(Query.All()) ?? new SessionModel();
        }, cancellationToken) ?? new SessionModel();
    }

    public async Task SaveSessionAsync(SessionModel session, CancellationToken cancellationToken = default)
    {
        await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<SessionModel>("session");
            collection.Upsert(session);
            return true;
        }, cancellationToken);
        Session = session;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    // TODO: Finish implementing this cache system
    public async Task AgentCacheSetAsync<T>(string agentUuid, string key, T value, CancellationToken cancellationToken = default)
    {
        await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<CacheEntry<T>>(agentUuid);
            var entry = new CacheEntry<T> { Key = key, Value = value, Timestamp = DateTime.UtcNow };
            collection.Upsert(entry);
            return true;
        }, cancellationToken);
    }

    public async Task<T?> AgentCacheGetAsync<T>(string agentUuid, string key, CancellationToken cancellationToken = default)
    {
        return await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<CacheEntry<T>>(agentUuid);
            var entry = collection.FindOne(x => x.Key == key);
            return entry != null ? entry.Value : default;
        }, cancellationToken);
    }

    public async Task GridCacheSetAsync<T>(string gridName, string key, T value, CancellationToken cancellationToken = default)
    {
        await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<CacheEntry<T>>("gridcache_" + gridName);
            var entry = new CacheEntry<T> { Key = key, Value = value, Timestamp = DateTime.UtcNow };
            collection.Upsert(entry);
            return true;
        }, cancellationToken);
    }

    public async Task<T?> GridCacheGetAsync<T>(string gridName, string key, CancellationToken cancellationToken = default)
    {
        return await ExecuteDbAsync(db =>
        {
            var collection = db.GetCollection<CacheEntry<T>>("gridcache_" + gridName);
            var entry = collection.FindOne(x => x.Key == key);
            return entry != null ? entry.Value : default;
        }, cancellationToken);
    }

    public class CacheEntry<T>
    {
        [BsonId]
        public string Key { get; set; } = string.Empty;
        public T? Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AvatarNameCacheEntry
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    void IDisposable.Dispose()
    {
        lock (_dbLock)
        {
            _database?.Dispose();
        }
    }

    public string? GetAvatarName(Guid id)
    {
        lock (_dbLock)
        {
            var col = _database!.GetCollection<AvatarNameCacheEntry>("avatar_name_cache");
            var entry = col.FindById(id);
            return entry?.Name;
        }
    }

    public void SetAvatarName(Guid id, string name)
    {
        lock (_dbLock)
        {
            var col = _database!.GetCollection<AvatarNameCacheEntry>("avatar_name_cache");
            col.Upsert(new AvatarNameCacheEntry { Id = id, Name = name });
        }
    }
}