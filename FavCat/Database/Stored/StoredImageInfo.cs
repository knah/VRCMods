using System;
using LiteDB;

namespace FavCat.Database.Stored
{
    public class StoredImageInfo
    {
        [BsonId] public string Id { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}