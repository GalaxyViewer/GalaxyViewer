using System.Collections.Generic;
using LiteDB;

namespace GalaxyViewer.Models;

public class PreferencesModel
{
    [BsonId] public ObjectId Id { get; set; }
    public string Theme { get; set; }
    public string LoginLocation { get; set; }
    public string Font { get; set; }
    public string Language { get; set; }
    public string SelectedGridNick { get; set; }
    public string AccentColor { get; set; } = "System Default";
    public long LastSavedEpoch { get; set; }
    public string Version { get; set; }
}