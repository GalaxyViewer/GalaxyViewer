using LiteDB;

namespace GalaxyViewer.Models;

public class GridModel
{
    [BsonId] public ObjectId Id { get; set; }
    public string GridNick { get; set; }
    public string GridName { get; set; }
    public string Platform { get; set; }
    public string LoginUri { get; set; }
    public string LoginPage { get; set; }
    public string HelperUri { get; set; }
    public string Website { get; set; }
    public string Support { get; set; }
    public string Register { get; set; }
    public string Password { get; set; }
    public string Version { get; set; }
}