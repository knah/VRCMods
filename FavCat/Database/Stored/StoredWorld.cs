using System;
using LiteDB;
using VRC.Core;

namespace FavCat.Database.Stored
{
    public class StoredWorld
    {
        [BsonId] public string WorldId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ReleaseStatus { get; set; }
        public string Platform { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
        public ApiModel.SupportedPlatforms SupportedPlatforms { get; set; }
        
        public string[] Tags { get; set; }
        public int Capacity { get; set; }
    }
}