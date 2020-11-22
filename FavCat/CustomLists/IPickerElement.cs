namespace FavCat.CustomLists
{
    public interface IPickerElement
    {
        string Id { get; }
        string Name { get; }
        string ImageUrl { get; }
        
        bool IsPrivate { get; }
        bool IsInaccessible { get; }
        bool SupportsDesktop { get; }
        bool SupportsQuest { get; }
    }
}