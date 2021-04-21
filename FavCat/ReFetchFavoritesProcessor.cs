using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FavCat.Database.Stored;
using LiteDB;
using MelonLoader;
using VRC.Core;
using Random = UnityEngine.Random;

namespace FavCat
{
    public static class ReFetchFavoritesProcessor
    {
        public static bool ImportRunning { get; private set; }

        public static string ImportStatusOuter { get; private set; } = "Not importing";
        public static string ImportStatusInner { get; private set; } = "";
        
        public static async Task ReFetchFavorites()
        {
            ImportRunning = true;
            ImportStatusOuter = "Re-fetch running...";

            var database = FavCatMod.Database;
            
            ImportStatusOuter = "Fetching worlds...";
            var worldFavs = database.WorldFavorites.myStoredFavorites.FindAll().ToList();
            for (var i = 0; i < worldFavs.Count; i++)
            {
                ImportStatusInner = i + "/" + worldFavs.Count;
                
                var storedFavorite = worldFavs[i];
                if (database.myStoredWorlds.FindById(storedFavorite.ObjectId) != null) continue;

                await FavCatMod.YieldToMainThread();

                new ApiWorld {id = storedFavorite.ObjectId}.Fetch(null, new Action<ApiContainer>(c =>
                {
                    if (c.Code == 404) database.CompletelyDeleteWorld(storedFavorite.ObjectId);
                }));

                await Task.Delay(TimeSpan.FromSeconds(5f + Random.Range(0f, 5f))).ConfigureAwait(false);
            }

            var canShowFavorites = DateTime.Now < FavCatMod.NoMoreVisibleAvatarFavoritesAfter;

            if (canShowFavorites)
            {
                ImportStatusOuter = "Fetching avatars...";
                var avatarFavs = database.AvatarFavorites.myStoredFavorites.FindAll().ToList();
                for (var i = 0; i < avatarFavs.Count; i++)
                {
                    ImportStatusInner = i + "/" + avatarFavs.Count;

                    var storedFavorite = avatarFavs[i];
                    if (database.myStoredAvatars.FindById(storedFavorite.ObjectId) != null) continue;

                    await FavCatMod.YieldToMainThread();

                    new ApiAvatar {id = storedFavorite.ObjectId}.Fetch(null, new Action<ApiContainer>(c =>
                    {
                        if (c.Code == 404) database.CompletelyDeleteAvatar(storedFavorite.ObjectId);
                    }));

                    await Task.Delay(TimeSpan.FromSeconds(5f + Random.Range(0f, 5f))).ConfigureAwait(false);
                }
            }

            ImportStatusOuter = "Fetching players...";
            var playerFavs = database.PlayerFavorites.myStoredFavorites.FindAll().ToList();
            for (var i = 0; i < playerFavs.Count; i++)
            {
                ImportStatusInner = i + "/" + playerFavs.Count;
                
                var storedFavorite = playerFavs[i];
                if (database.myStoredPlayers.FindById(storedFavorite.ObjectId) != null) continue;

                await FavCatMod.YieldToMainThread();

                new APIUser {id = storedFavorite.ObjectId}.Fetch(new Action<ApiContainer>(c =>
                {
                    if (c.Code == 404) database.CompletelyDeletePlayer(storedFavorite.ObjectId);
                }));

                await Task.Delay(TimeSpan.FromSeconds(5f + Random.Range(0f, 5f))).ConfigureAwait(false);
            }
            
            ImportRunning = false;
        }
    }
}