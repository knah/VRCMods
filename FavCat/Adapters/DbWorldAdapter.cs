using FavCat.CustomLists;
using FavCat.Database.Stored;
using VRC.Core;

namespace FavCat.Adapters
{
    public class DbWorldAdapter : IPickerElement, IStoredModelAdapter<StoredWorld>
    {
        private readonly StoredWorld myWorld;
        private readonly StoredFavorite? myFavorite;

        public DbWorldAdapter(StoredWorld world, StoredFavorite? favorite)
        {
            myWorld = world;
            myFavorite = favorite;
        }

        public string Id => myWorld.WorldId;
        public string Name => myWorld.Name;
        public string ImageUrl => myWorld.ThumbnailUrl;
        public bool IsPrivate => myWorld.ReleaseStatus != "public";
        public bool IsInaccessible => false;

        public bool SupportsDesktop => (myWorld.SupportedPlatforms & ApiModel.SupportedPlatforms.StandaloneWindows) != 0;
        public bool SupportsQuest => (myWorld.SupportedPlatforms & ApiModel.SupportedPlatforms.Android) != 0;
        
        public StoredWorld Model => myWorld;
        public StoredFavorite? StoredFavorite => myFavorite;
    }
}