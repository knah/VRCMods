namespace FavCat.Adapters
{
    public interface IStoredModelAdapter<T> where T: class
    {
        T Model { get; }
    }
}