using GalaxyViewer.Models;
using LiteDB;

namespace GalaxyViewer.Services;

public interface ILiteDbService
{
    LiteDatabase Database { get; }
    SessionModel GetSession();
    void SaveSession(SessionModel session);
    ILiteCollection<SessionModel> GetCollection<SessionModel>(string name);
}