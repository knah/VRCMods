using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FavCat.Database.Stored;
using MelonLoader;
using VRC.Core;

namespace FavCat.Database
{
    public partial class LocalStoreDatabase
    {
        public void UpdateStoredPlayer(APIUser player)
        {
            if (string.IsNullOrEmpty(player.id) || string.IsNullOrEmpty(player.displayName))
                return;
            
            myUpdateThreadQueue.Enqueue(() =>
            {
                var storedPlayer = new StoredPlayer
                {
                    PlayerId = player.id,
                    Name = player.displayName,
                    ThumbnailUrl = player.currentAvatarThumbnailImageUrl
                };

                myStoredPlayers.Upsert(storedPlayer);
            });
        }
        
        internal void RunBackgroundPlayerSearch(string text, Action<IEnumerable<StoredPlayer>> callback)
        {
            MelonLogger.Log($"Running local player search for text {text}");
            Task.Run(() => {
                var list = new List<StoredPlayer>();
                foreach (var stored in myStoredPlayers.FindAll())
                {
                    if (stored.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                        list.Add(stored);
                }

                callback(list);
            }).NoAwait();
        }
        
        public void CompletelyDeletePlayer(string userId)
        {
            myStoredPlayers.Delete(userId);
            PlayerFavorites.DeleteFavoriteFromAllCategories(userId);
        }
    }
}