using LiteDB;

namespace GalaxyViewer.Models
{
    public class PreferencesModel
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Theme { get; set; }
        public string LoginLocation { get; set; }
        public string Font { get; set; }
        public string Language { get; set; }
        public long LastSavedEpoch { get; set; }
    }
}