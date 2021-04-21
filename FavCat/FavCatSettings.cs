using LiteDB;
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
        private const string UseSharedMode = "UseSharedMode";
        private const string LastAnnoyingMessageSeen = "AnnoyingMessageSeen";
        private const string MakeClickSounds = "MakeClickSounds";
        private const string EnableAvatarFavs = "EnableAvatarFavs";
        private const string EnableWorldFavs = "EnableWorldFavs";
        private const string EnablePlayerFavs = "EnablePlayerFavs";

        internal static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(FavCatCategory, "FavCat");

            MelonPrefs.RegisterString(FavCatCategory, DatabasePathSettings, "./UserData", "Database directory path", true);
            MelonPrefs.RegisterString(FavCatCategory, ImageCacheSettings, "./UserData", "Image cache directory path", true);
            
            MelonPrefs.RegisterBool(FavCatCategory, EnableAvatarFavs, true, "Enable avatar favorites (restart required)");
            MelonPrefs.RegisterBool(FavCatCategory, EnableWorldFavs, true, "Enable world favorites (restart required)");
            MelonPrefs.RegisterBool(FavCatCategory, EnablePlayerFavs, true, "Enable player favorites (restart required)");
            
            MelonPrefs.RegisterString(FavCatCategory, ImageCachingMode, "full", "Image caching mode");
            MelonPrefs.RegisterInt(FavCatCategory, ImageCacheMaxSize, 4096, "Image cache max size (MB)");
            MelonPrefs.RegisterBool(FavCatCategory, HidePopupAfterFav, true, "Hide favorite popup after (un)favoriting a world or a player");

            MelonPrefs.RegisterBool(FavCatCategory, MakeClickSounds, true, "Click sounds");
            
            // shared mode can't be multi-threaded, so hide this until other parts of code are ready for ST patterns
            MelonPrefs.RegisterBool(FavCatCategory, UseSharedMode, false, "Support multiple VRC instances (slower, restart required)", true);
            
            MelonPrefs.RegisterString(FavCatCategory, LastAnnoyingMessageSeen, "", hideFromList: true);
            
            ExpansionKitApi.RegisterSettingAsStringEnum(FavCatCategory, ImageCachingMode, new []{("full", "Full local image cache (fastest, safest)"), ("fast", "Fast, use more RAM"), ("builtin", "Preserve RAM, more API requests")});
        }

        public static string DatabasePath => MelonPrefs.GetString(FavCatCategory, DatabasePathSettings);
        public static string ImageCachePath => MelonPrefs.GetString(FavCatCategory, ImageCacheSettings);
        public static bool UseLocalImageCache => MelonPrefs.GetString(FavCatCategory, ImageCachingMode) == "full";
        public static bool CacheImagesInMemory => MelonPrefs.GetString(FavCatCategory, ImageCachingMode) == "fast";
        public static long MaxCacheSizeBytes => MelonPrefs.GetInt(FavCatCategory, ImageCacheMaxSize) * 1024L * 1024L;

        public static bool IsHidePopupAfterFav => MelonPrefs.GetBool(FavCatCategory, HidePopupAfterFav);

        public static ConnectionType ConnectionType => MelonPrefs.GetBool(FavCatCategory, UseSharedMode)
            ? ConnectionType.Shared
            : ConnectionType.Direct;

        public static bool IsSingleThreadedMode => MelonPrefs.GetBool(FavCatCategory, UseSharedMode);

        public static bool IsEnableAvatarFavs => MelonPrefs.GetBool(FavCatCategory, EnableAvatarFavs);
        public static bool IsEnableWorldFavs => MelonPrefs.GetBool(FavCatCategory, EnableWorldFavs);
        public static bool IsEnablePlayerFavs => MelonPrefs.GetBool(FavCatCategory, EnablePlayerFavs);

        public static bool DoClickSounds => MelonPrefs.GetBool(FavCatCategory, MakeClickSounds);

        public static string DontShowAnnoyingMessage
        {
            get => MelonPrefs.GetString(FavCatCategory, LastAnnoyingMessageSeen);
            set
            {
                MelonPrefs.SetString(FavCatCategory, LastAnnoyingMessageSeen, value);
                MelonPrefs.SaveConfig();
            }
        }
    }
}