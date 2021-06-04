using MelonLoader;
using UIExpansionKit.API;

#nullable disable

namespace FavCat
{
    public static class FavCatSettings
    {
        private const string SettingsCategory = "FavCat";
        internal static MelonPreferences_Entry<string> DatabasePath;
        internal static MelonPreferences_Entry<string> ImageCachePath;
        private static MelonPreferences_Entry<string> AnnoyingMessageSeen;
        
        internal static MelonPreferences_Entry<bool> EnableAvatarFavs;
        internal static MelonPreferences_Entry<bool> EnableWorldFavs;
        internal static MelonPreferences_Entry<bool> EnablePlayerFavs;
        
        private static MelonPreferences_Entry<string> ImageCacheMode;
        private static MelonPreferences_Entry<int> ImageCacheMaxSize;
        internal static MelonPreferences_Entry<bool> HidePopupAfterFav;
        
        internal static MelonPreferences_Entry<bool> MakeClickSounds;
        internal static MelonPreferences_Entry<string> AvatarSearchMode;
        internal static MelonPreferences_Entry<bool> SortPlayersByOnline;

        internal static void RegisterSettings()
        {
            var avatarSearchModeName = "AvatarSearchMode";
            
            var category = MelonPreferences.CreateCategory(SettingsCategory, "FavCat");
            
            DatabasePath = (MelonPreferences_Entry<string>) category.CreateEntry("DatabasePath", "./UserData", "Database directory path", true);
            ImageCachePath = (MelonPreferences_Entry<string>) category.CreateEntry("ImageCachePath", "./UserData", "Image cache directory path", true);
            AnnoyingMessageSeen = (MelonPreferences_Entry<string>) category.CreateEntry("AnnoyingMessageSeen", "", is_hidden: true);
            
            EnableAvatarFavs = (MelonPreferences_Entry<bool>) category.CreateEntry("EnableAvatarFavs", true, "Enable avatar favorites (restart required)");
            EnableWorldFavs = (MelonPreferences_Entry<bool>) category.CreateEntry("EnableWorldFavs", true, "Enable world favorites (restart required)");
            EnablePlayerFavs = (MelonPreferences_Entry<bool>) category.CreateEntry("EnablePlayerFavs", true, "Enable player favorites (restart required)");

            ImageCacheMode = (MelonPreferences_Entry<string>) category.CreateEntry("ImageCachingMode", "full", "Image caching mode");
            ImageCacheMaxSize = (MelonPreferences_Entry<int>) category.CreateEntry("ImageCacheMaxSize", 4096, "Image cache max size (MB)");
            HidePopupAfterFav = (MelonPreferences_Entry<bool>) category.CreateEntry("HidePopupAfterFav", true, "Hide favorite popup after (un)favoriting a world or a player");
            
            MakeClickSounds = (MelonPreferences_Entry<bool>) category.CreateEntry("MakeClickSounds", true, "Click sounds");
            AvatarSearchMode = (MelonPreferences_Entry<string>) category.CreateEntry(avatarSearchModeName, "select", "Avatar search result action");
            SortPlayersByOnline = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(SortPlayersByOnline), true, "Show offline players at the end of the list");
            
            ExpansionKitApi.RegisterSettingAsStringEnum(SettingsCategory, "ImageCachingMode", new []{("full", "Full local image cache (fastest, safest)"), ("fast", "Fast, use more RAM"), ("builtin", "Preserve RAM, more API requests")});
            ExpansionKitApi.RegisterSettingAsStringEnum(SettingsCategory, avatarSearchModeName, new []{("select", "Select avatar"), ("author", "Show avatar author (safer)")});
        }

        public static bool UseLocalImageCache => ImageCacheMode.Value == "full";
        public static bool CacheImagesInMemory => ImageCacheMode.Value == "fast";
        public static long MaxCacheSizeBytes => ImageCacheMaxSize.Value * 1024L * 1024L;

        public static string DontShowAnnoyingMessage
        {
            get => AnnoyingMessageSeen.Value;
            set
            {
                AnnoyingMessageSeen.Value = value;
                MelonPreferences.Save();
            }
        }
    }
}