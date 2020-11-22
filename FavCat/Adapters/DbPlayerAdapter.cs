using FavCat.CustomLists;
using FavCat.Database.Stored;

namespace FavCat.Adapters
{
    public class DbPlayerAdapter : IPickerElement, IStoredModelAdapter<StoredPlayer>
    {
        private readonly StoredPlayer myPlayer;
        private readonly StoredFavorite? myFavorite;

        public DbPlayerAdapter(StoredPlayer player, StoredFavorite? favorite)
        {
            myPlayer = player;
            myFavorite = favorite;
        }

        public string Id => myPlayer.PlayerId;
        public string Name => myPlayer.Name;
        public string ImageUrl => myPlayer.ThumbnailUrl;
        public bool IsPrivate => false;
        public bool IsInaccessible => false;
        public bool SupportsDesktop => false;
        public bool SupportsQuest => false;
        public StoredPlayer Model => myPlayer;
        public StoredFavorite? StoredFavorite => myFavorite;
    }
}