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
        internal void RunBackgroundWorldSearch(string text, Action<IEnumerable<StoredWorld>> callback)
        {
            MelonLogger.Log($"Running local world search for text {text}");
            Task.Run(() => {
                var list = new List<StoredWorld>();
                foreach (var stored in myStoredWorlds.FindAll())
                {
                    if (stored.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        (stored.Description ?? "").IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        stored.AuthorName.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                        list.Add(stored);
                }

                callback(list);
            }).NoAwait();
        }
        
        public void UpdateStoredWorld(ApiWorld world)
        {
            if (string.IsNullOrEmpty(world.id) || string.IsNullOrEmpty(world.name)) return;

            myUpdateThreadQueue.Enqueue(() =>
            {
                var preExisting = myStoredWorlds.FindById(world.id);
                if (preExisting != null)
                {
                    if (world.assetUrl == null) world.supportedPlatforms = preExisting.SupportedPlatforms;
                
                    world.description ??= preExisting.Description;
                }
                
                var storedWorld = new StoredWorld
                {
                    WorldId = world.id,
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