using LiteDB;

#nullable disable

namespace FavCat.Database.Stored
{
    public class StoredCategory
    {
        [BsonId] public string CategoryName { get; set; }
        public string SortType { get; set; }
        public int VisibleRows { get; set; }
    }
}