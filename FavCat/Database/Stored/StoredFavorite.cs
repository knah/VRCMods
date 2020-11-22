using System;
using LiteDB;

namespace FavCat.Database.Stored
{
    public class StoredFavorite
    {
        [BsonId] public int FavoriteId { get; set; }
        
        public string ObjectId { get; set; }
        public string Category { get; set; }
        public DateTime AddedOn { get; set; }
    }
}