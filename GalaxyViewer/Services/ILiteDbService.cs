using GalaxyViewer.Models;
using LiteDB;

namespace GalaxyViewer.Services;

public interface ILiteDbService
{
    LiteDatabase Database { get; }
    ILiteCollection<T> GetCollection<T>(string name);
    SessionModel GetSession();
    void SaveSession(SessionModel session);
}