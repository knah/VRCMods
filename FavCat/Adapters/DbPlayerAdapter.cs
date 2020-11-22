using FavCat.CustomLists;
using FavCat.Database.Stored;

namespace FavCat.Adapters
{
    public class DbPlayerAdapter : IPickerElement, IStoredModelAdapter<StoredPlayer>
    {
        private readonly StoredPlayer myPlayer;

        public DbPlayerAdapter(StoredPlayer player)
        {
            myPlayer = player;
        }

        public string Id => myPlayer.PlayerId;
        public string Name => myPlayer.Name;
        public string ImageUrl => myPlayer.ThumbnailUrl;
        public bool IsPrivate => false;
        public bool IsInaccessible => false;
        public bool SupportsDesktop => false;
        public bool SupportsQuest => false;
        public StoredPlayer Model => myPlayer;
    }
}