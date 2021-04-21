namespace FavCat.Database.Stored
{
    public interface INamedStoredObject
    {
        string Name { get; }
        string? AuthorName { get; }
    }
}