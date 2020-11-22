using FavCat.CustomLists;
using VRC.Core;

namespace FavCat.Adapters
{
    internal class ApiAvatarAdapter : IPickerElement
    {
        private readonly ApiAvatar myAvatar;

        public ApiAvatarAdapter(ApiAvatar avatar)
        {
            myAvatar = avatar;
        }

        public string Name => myAvatar.name;
        public string ImageUrl => myAvatar.thumbnailImageUrl;
        public string Id => myAvatar.id;
        public bool IsPrivate => myAvatar.releaseStatus != "public";
        public bool SupportsDesktop => myAvatar.supportedPlatforms == ApiModel.SupportedPlatforms.StandaloneWindows || myAvatar.supportedPlatforms == ApiModel.SupportedPlatforms.All;
        public bool SupportsQuest => myAvatar.supportedPlatforms == ApiModel.SupportedPlatforms.Android || myAvatar.supportedPlatforms == ApiModel.SupportedPlatforms.All;
        public bool IsInaccessible => IsPrivate && myAvatar.authorId != APIUser.CurrentUser.id;
    }
}