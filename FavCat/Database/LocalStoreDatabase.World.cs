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
        internal void RunBackgroundWorldSearch(string text, Action<IEnumerable<StoredWorld>> callback)
        {
            MelonLogger.Msg($"Running local world search for text {text}");
            Task.Run(() => {
                var searchText = text.ToLowerInvariant();
                var list = myStoredWorlds.Find(stored =>
                    stored.Name.ToLower().Contains(searchText) ||
                    stored.Description != null && stored.Description.ToLower().Contains(searchText) ||
                    stored.AuthorName.ToLower().Contains(searchText)).ToList();

                callback(list);
            }).NoAwait();
        }
        
        public void UpdateStoredWorld(ApiWorld world)
        {
            var id = world.id;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(world.name)) return;
            
            var storedWorld = new StoredWorld
            {
                WorldId = id,
                AuthorId = world.authorId,
                Name = world.name,
                Description = world.description,
                AuthorName = world.authorName,
                ThumbnailUrl = world.thumbnailImageUrl,
                ImageUrl = world.imageUrl,
                ReleaseStatus = world.releaseStatus,
                Platform = world.platform,
                Version = world.version,
                CreatedAt = DateTime.FromBinary(world.created_at.ToBinary()),
                UpdatedAt = DateTime.FromBinary(world.updated_at.ToBinary()),
                SupportedPlatforms = world.supportedPlatforms,

                Capacity = world.capacity,
                Tags = world.tags.ToArray(),
            };

            var hasNoAssetUrl = world.assetUrl == null;

            myUpdateThreadQueue.Enqueue(() =>
            {
                var preExisting = myStoredWorlds.FindById(id);
                if (preExisting != null)
                {
                    if (hasNoAssetUrl) storedWorld.SupportedPlatforms = preExisting.SupportedPlatforms;
                
                    storedWorld.Description ??= preExisting.Description;
                }

                myStoredWorlds.Upsert(storedWorld);
            });
        }
        
        public void CompletelyDeleteWorld(string worldId)
        {
            myStoredWorlds.Delete(worldId);
            WorldFavorites.DeleteFavoriteFromAllCategories(worldId);
        }
    }
}