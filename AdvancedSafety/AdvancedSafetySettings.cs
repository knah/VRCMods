using System.Reflection;
using MelonLoader;

namespace AdvancedSafety
{
    public static class AdvancedSafetySettings
    {
        private const string SettingsCategory = "AdvancedSafety";

        public static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(SettingsCategory, "Advanced safety");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AvatarFilteringEnabled), true, "Enable avatar filtering");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(IncludeFriends), false, "Friends are affected by avatar filtering");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AbideByShowAvatar), true, "\"Show avatar\" bypasses avatar filtering");

            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AvatarFilteringOnlyInPublic), false, "Do avatar filtering only in public instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(IncludeFriendsInHides), false, "Friends are affected by avatar hiding");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(HidesAbideByShowAvatar), true, "\"Show avatar\" bypasses avatar hiding");

            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AllowSpawnSounds), false, "Allow avatar spawn sounds");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AllowGlobalSounds), false, "Allow global sounds on avatars");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxAudioSources), 16, "Max audio sources");

            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxPolygons), 2_000_000, "Max polygons");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxMaterialSlots), 100, "Max material slots");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(HeuristicallyRemoveScreenSpaceBullshit), true, "Try to remove fullscreen effects");
            
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxConstraints), 200, "Max constraints");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxColliders), 32, "Max colliders");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxRigidBodies), 32, "Max rigidbodies");

            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxClothVertices), 10_000, "Max cloth vertices");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxTransforms), 1000, "Max bones/transforms");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxAnimators), 64, "Max animators");
            
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxLights), 2, "Max lights");
            ModPrefs.RegisterPrefInt(SettingsCategory, nameof(MaxComponents), 4_000, "Max total components");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(AllowUiLayer), false, "Allow UI layer on avatars");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(HidePortalsFromBlockedUsers), true, "Hide portals from blocked users");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(HidePortalsFromNonFriends), false, "Hide portals from non-friends");
            ModPrefs.RegisterPrefBool(SettingsCategory, nameof(HidePortalsCreatedTooClose), true, "Hide portals created too close to local player");
            
            OnModSettingsApplied();
        }

        public static bool AvatarFilteringEnabled;
        public static bool AvatarFilteringOnlyInPublic;
        
        public static bool AllowSpawnSounds;
        public static bool AllowGlobalSounds;
        
        public static bool IncludeFriends;
        public static bool IncludeFriendsInHides;
        
        public static bool AbideByShowAvatar;
        public static bool HidesAbideByShowAvatar;
        
        public static bool AllowUiLayer;

        public static int MaxPolygons;
        public static int MaxTransforms;
        public static int MaxConstraints;
        public static int MaxMaterialSlots;
        public static int MaxAudioSources;
        public static int MaxClothVertices;
        public static int MaxColliders;
        public static int MaxRigidBodies;
        public static int MaxAnimators;
        public static int MaxLights;
        public static int MaxComponents;

        public static bool HeuristicallyRemoveScreenSpaceBullshit;

        public static bool HidePortalsFromBlockedUsers;
        public static bool HidePortalsFromNonFriends;
        public static bool HidePortalsCreatedTooClose;

        public static void OnModSettingsApplied()
        {
            foreach (var fieldInfo in typeof(AdvancedSafetySettings).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (fieldInfo.FieldType == typeof(int))
                    fieldInfo.SetValue(null, ModPrefs.GetInt(SettingsCategory, fieldInfo.Name));
                
                if (fieldInfo.FieldType == typeof(bool))
                    fieldInfo.SetValue(null, ModPrefs.GetBool(SettingsCategory, fieldInfo.Name));
            }
        }
    }
}