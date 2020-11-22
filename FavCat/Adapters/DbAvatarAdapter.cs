using FavCat.CustomLists;
using FavCat.Database.Stored;
using VRC.Core;

namespace FavCat.Adapters
{
    internal class DbAvatarAdapter : IPickerElement, IStoredModelAdapter<StoredAvatar>
    {
        private readonly StoredAvatar myAvatar;
        private readonly StoredFavorite? myFavorite;

        public DbAvatarAdapter(StoredAvatar avatar, StoredFavorite? favorite)
        {
            myAvatar = avatar;
            myFavorite = favorite;
        }

        public string Name => myAvatar.Name;
        public string ImageUrl => myAvatar.ThumbnailUrl;
        public string Id => myAvatar.AvatarId;
        public bool IsPrivate => myAvatar.ReleaseStatus != "public";
        public bool SupportsDesktop => myAvatar.SupportedPlatforms == ApiModel.SupportedPlatforms.StandaloneWindows || myAvatar.SupportedPlatforms == ApiModel.SupportedPlatforms.All;
        public bool SupportsQuest => myAvatar.SupportedPlatforms == ApiModel.SupportedPlatforms.Android || myAvatar.SupportedPlatforms == ApiModel.SupportedPlatforms.All;
        public bool IsInaccessible => IsPrivate && myAvatar.AuthorId != APIUser.CurrentUser.id;

        public StoredAvatar Model => myAvatar;
        public StoredFavorite? StoredFavorite => myFavorite;
    }
}