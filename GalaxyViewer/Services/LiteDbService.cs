using LiteDB;
using System;
using System.IO;

namespace GalaxyViewer.Services;

public class LiteDbService : IDisposable
{
    private readonly LiteDatabase _database;

    public LiteDbService()
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer", "data.db");
        _database = new LiteDatabase(dbPath);
    }

    public ILiteCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    public void Dispose()
    {
        _database?.Dispose();
    }
}