using FavCat.Database.Stored;

namespace FavCat.Adapters
{
    public interface IStoredModelAdapter<T> where T: class
    {
        T Model { get; }
        StoredFavorite? StoredFavorite { get; }
    }
}