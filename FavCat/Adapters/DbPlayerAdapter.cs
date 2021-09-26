using FavCat.CustomLists;
using FavCat.Database.Stored;
using FavCat.Modules;
using UnityEngine;

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
        public bool IsPrivate => PlayersModule.GetOnlineApiUser(Id)?.location == "private";
        public bool IsInaccessible => false;
        public bool SupportsDesktop => false;
        public bool SupportsQuest => false;
        public StoredPlayer Model => myPlayer;
        public StoredFavorite? StoredFavorite => myFavorite;

        public Color? CornerIconColor
        {
            get
            {
                var onlineUser = PlayersModule.GetOnlineApiUser(Id);
                if (onlineUser == null)
                    return Color.gray;

                switch (onlineUser.status)
                {
                    case "join me":
                        return new Color(0, 0.75f, 0.75f, 1);
                    case "online":
                    case "active":
                        return new Color(0, 0.75f, 0, 1);
                    case "ask me":
                        return new Color(1f, 0.5f, 0, 1);
                    case "busy":
                        return new Color(0.75f, 0, 0, 1);
                    default:
                        return new Color(0.5f, 0.5f, 0, 1);
                }
            }
        }
    }
}