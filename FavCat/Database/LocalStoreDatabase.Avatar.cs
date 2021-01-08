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
        internal void RunBackgroundAvatarSearch(string text, Action<IEnumerable<StoredAvatar>> callback)
        {
            MelonLogger.Log($"Running local avatar search for text {text}");
            var ownerId = APIUser.CurrentUser.id;
            Task.Run(() => {
                var list = new List<StoredAvatar>();
                foreach (var stored in myStoredAvatars.FindAll())
                {
                    if (stored.ReleaseStatus != "public" && stored.AuthorId != ownerId) continue;

                    if (stored.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        (stored.Description ?? "").IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        stored.AuthorName.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                        list.Add(stored);
                }

                callback(list);
            }).NoAwait();
        }
        
        internal void RunBackgroundAvatarSearchByUser(string userId, Action<IEnumerable<StoredAvatar>> callback)
        {
            MelonLogger.Log($"Running local avatar search for user {userId}");
            var ownerId = APIUser.CurrentUser.id;
            Task.Run(() => {
                var list = new List<StoredAvatar>();
                foreach (var stored in myStoredAvatars.FindAll())
                {
                    if (stored.ReleaseStatus != "public" && stored.AuthorId != ownerId) continue;

                    if (stored.AuthorId == userId)
                        list.Add(stored);
                }

                callback(list);
            }).NoAwait();
        }
        
        

        private StoredAvatar? GetAvatar(string id)
        {
            return myStoredAvatars.FindById(id);
        }

        public void UpdateStoredAvatar(ApiAvatar avatar)
        {
            if (string.IsNullOrEmpty(avatar.id) || string.IsNullOrEmpty(avatar.name)) return;
            
            myUpdateThreadQueue.Enqueue(() =>
            {
                var preExisting = GetAvatar(avatar.id);
            
                if (preExisting != null)
                {
                    if (avatar.assetUrl == null) avatar.supportedPlatforms = preExisting.SupportedPlatforms;
                
                    avatar.description ??= preExisting.Description;
                }

                var storedAvatar = new StoredAvatar
                {
                    AvatarId = avatar.id,
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