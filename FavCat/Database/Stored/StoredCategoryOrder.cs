using System.Collections.Generic;
using LiteDB;

namespace FavCat.Database.Stored
{
    public struct CategoryInfo
    {
        public string Name { get; set; }
        public bool IsExternal { get; set; }
    }
    
    public class StoredCategoryOrder
    {
        [BsonId] public DatabaseEntity EntityType { get; set; }
        public List<CategoryInfo> Order { get; set; } = new List<CategoryInfo>();
        public List<string> DefaultListsToHide { get; set; } = new List<string>();
    }
}