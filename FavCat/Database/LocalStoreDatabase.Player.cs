using System;
using System.Collections.Generic;
using System.Linq;
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
                var searchText = text.ToLowerInvariant();
                var list = myStoredPlayers.Find(stored => stored.Name.Contains(searchText)).ToList();

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