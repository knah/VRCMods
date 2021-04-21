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
        internal void RunBackgroundAvatarSearch(string text, Action<IEnumerable<StoredAvatar>> callback)
        {
            MelonLogger.Log($"Running local avatar search for text {text}");
            var ownerId = APIUser.CurrentUser.id;
            Task.Run(() => {
                var searchText = text.ToLowerInvariant();
                var list = myStoredAvatars
                    .Find(stored =>
                        (stored.Name.ToLower().Contains(searchText) ||
                        stored.Description != null && stored.Description.ToLower().Contains(searchText) ||
                        stored.AuthorName.ToLower().Contains(searchText)) 
                        && (stored.ReleaseStatus == "public" || stored.AuthorId == ownerId))
                    .ToList();

                callback(list);
            }).NoAwait();
        }
        
        internal void RunBackgroundAvatarSearchByUser(string userId, Action<IEnumerable<StoredAvatar>> callback)
        {
            MelonLogger.Log($"Running local avatar search for user {userId}");
            var ownerId = APIUser.CurrentUser.id;
            Task.Run(() => {
                var list = myStoredAvatars.Find(stored =>
                        stored.AuthorId == userId && (stored.ReleaseStatus == "public" || stored.AuthorId == ownerId))
                    .ToList();

                callback(list);
            }).NoAwait();
        }

        private StoredAvatar? GetAvatar(string id)
        {
            return myStoredAvatars.FindById(id);
        }

        public void UpdateStoredAvatar(ApiAvatar avatar)
        {
            var id = avatar.id;
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(avatar.name)) return;
            
            var storedAvatar = new StoredAvatar
            {
                AvatarId = id,
                AuthorId = avatar.authorId,
                Name = avatar.name,
                Description = avatar.description,
                AuthorName = avatar.authorName,
                ThumbnailUrl = avatar.thumbnailImageUrl,
                ImageUrl = avatar.imageUrl,
                ReleaseStatus = avatar.releaseStatus,
                Platform = avatar.platform,
                CreatedAt = DateTime.FromBinary(avatar.created_at.ToBinary()),
                UpdatedAt = DateTime.FromBinary(avatar.updated_at.ToBinary()),
                SupportedPlatforms = avatar.supportedPlatforms
            };
            
            myUpdateThreadQueue.Enqueue(() =>
            {
                var preExisting = GetAvatar(id);
            
                if (preExisting != null)
                {
                    if (preExisting.UpdatedAt > storedAvatar.UpdatedAt) return;
                    
                    if (avatar.assetUrl == null) storedAvatar.SupportedPlatforms = preExisting.SupportedPlatforms;
                
                    storedAvatar.Description ??= preExisting.Description;
                }

                myStoredAvatars.Upsert(storedAvatar);
            });
        }

        public void CompletelyDeleteAvatar(string avatarId)
        {
            myStoredAvatars.Delete(avatarId);
            AvatarFavorites.DeleteFavoriteFromAllCategories(avatarId);
        }
    }
}