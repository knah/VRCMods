using MelonLoader;
using UIExpansionKit.API;

namespace FavCat
{
    public static class FavCatSettings
    {
        private const string FavCatCategory = "FavCat";
        private const string DatabasePathSettings = "DatabasePath";
        private const string ImageCacheSettings = "ImageCachePath";
        private const string ImageCachingMode = "ImageCachingMode";
        private const string ImageCacheMaxSize = "ImageCacheMaxSize";
        private const string HidePopupAfterFav = "HidePopupAfterFav";

        internal static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(FavCatCategory, "FavCat");

            MelonPrefs.RegisterString(FavCatCategory, DatabasePathSettings, "./UserData", "Database directory path", true);
            MelonPrefs.RegisterString(FavCatCategory, ImageCacheSettings, "./UserData", "Image cache directory path", true);
            MelonPrefs.RegisterString(FavCatCategory, ImageCachingMode, "full", "Image caching mode");
            MelonPrefs.RegisterInt(FavCatCategory, ImageCacheMaxSize, 4096, "Image cache max size (MB)");
            MelonPrefs.RegisterBool(FavCatCategory, HidePopupAfterFav, true, "Hide favorite popup after (un)favoriting a world or a player");
            
            ExpansionKitApi.RegisterSettingAsStringEnum(FavCatCategory, ImageCachingMode, new []{("full", "Full local image cache (fastest, safest)"), ("fast", "Fast, use more RAM"), ("builtin", "Preserve RAM, more API requests")});
        }

        public static string DatabasePath => MelonPrefs.GetString(FavCatCategory, DatabasePathSettings);
        public static string ImageCachePath => MelonPrefs.GetString(FavCatCategory, ImageCacheSettings);
        public static bool UseLocalImageCache => MelonPrefs.GetString(FavCatCategory, ImageCachingMode) == "full";
        public static bool CacheImagesInMemory => MelonPrefs.GetString(FavCatCategory, ImageCachingMode) == "fast";
        public static long MaxCacheSizeBytes => MelonPrefs.GetInt(FavCatCategory, ImageCacheMaxSize) * 1024L * 1024L;

        public static bool IsHidePopupAfterFav => MelonPrefs.GetBool(FavCatCategory, HidePopupAfterFav);

    }
}