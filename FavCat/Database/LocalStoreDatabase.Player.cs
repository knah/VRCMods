using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using VRC.Core;

namespace FavCat.Database
{
    public partial class LocalStoreDatabase
    {
        public void UpdateStoredPlayer(APIUser player)
        {
            if (string.IsNullOrEmpty(player.id) || string.IsNullOrEmpty(player.displayName))
                return;
            
            var storedPlayer = new StoredPlayer
            {
                PlayerId = player.id,
                Name = player.displayName,
                ThumbnailUrl = player.profilePicThumbnailImageUrl // already includes override/avatar check
            };
            
            myUpdateThreadQueue.Enqueue(() =>
            {
                myStoredPlayers.Upsert(storedPlayer);
            });
        }
        
        internal void RunBackgroundPlayerSearch(string text, Action<IEnumerable<StoredPlayer>> callback)
        {
            FavCatMod.Logger.Msg($"Running local player search for text {text}");
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