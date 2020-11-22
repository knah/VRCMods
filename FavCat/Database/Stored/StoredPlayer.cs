using LiteDB;

namespace FavCat.Database.Stored
{
    public class StoredPlayer
    {
        [BsonId] public string PlayerId { get; set; }
        public string Name { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}