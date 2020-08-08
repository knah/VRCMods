using System.Reflection;
using MelonLoader;

namespace AdvancedSafety
{
    public static class AdvancedSafetySettings
    {
        private const string SettingsCategory = "AdvancedSafety";

        public static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(SettingsCategory, "Advanced safety");
            
            MelonPrefs.RegisterBool(SettingsCategory, nameof(AvatarFilteringEnabled), true, "Enable avatar filtering");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(IncludeFriends), false, "Friends are affected by avatar filtering");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(AbideByShowAvatar), true, "\"Show avatar\" bypasses avatar filtering");

            MelonPrefs.RegisterBool(SettingsCategory, nameof(AvatarFilteringOnlyInPublic), false, "Do avatar filtering only in public instances");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(IncludeFriendsInHides), false, "Friends are affected by avatar hiding");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(HidesAbideByShowAvatar), true, "\"Show avatar\" bypasses avatar hiding");

            MelonPrefs.RegisterBool(SettingsCategory, nameof(AllowSpawnSounds), false, "Allow avatar spawn sounds");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(AllowGlobalSounds), false, "Allow global sounds on avatars");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxAudioSources), 16, "Max audio sources");

            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxPolygons), 2_000_000, "Max polygons");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxMaterialSlots), 100, "Max material slots");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(HeuristicallyRemoveScreenSpaceBullshit), true, "Try to remove fullscreen effects");
            
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxConstraints), 200, "Max constraints");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxColliders), 32, "Max colliders");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxRigidBodies), 32, "Max rigidbodies");

            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxClothVertices), 10_000, "Max cloth vertices");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxTransforms), 1000, "Max bones/transforms");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxAnimators), 64, "Max animators");
            
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxLights), 2, "Max lights");
            MelonPrefs.RegisterInt(SettingsCategory, nameof(MaxComponents), 4_000, "Max total components");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(AllowUiLayer), false, "Allow UI layer on avatars");
            
            MelonPrefs.RegisterBool(SettingsCategory, nameof(HidePortalsFromBlockedUsers), true, "Hide portals from blocked users");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(HidePortalsFromNonFriends), false, "Hide portals from non-friends");
            MelonPrefs.RegisterBool(SettingsCategory, nameof(HidePortalsCreatedTooClose), true, "Hide portals created too close to local player");
            
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
                    fieldInfo.SetValue(null, MelonPrefs.GetInt(SettingsCategory, fieldInfo.Name));
                
                if (fieldInfo.FieldType == typeof(bool))
                    fieldInfo.SetValue(null, MelonPrefs.GetBool(SettingsCategory, fieldInfo.Name));
            }
        }
    }
}