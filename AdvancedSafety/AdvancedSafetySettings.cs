using MelonLoader;

namespace AdvancedSafety
{
    public static class AdvancedSafetySettings
    {
        private const string SettingsCategory = "AdvancedSafety";

        public static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory(SettingsCategory, "Advanced safety");
            
            AvatarFilteringEnabled = (MelonPreferences_Entry<bool>) category.CreateEntry( nameof(AvatarFilteringEnabled), true, "Enable avatar filtering");
            IncludeFriends = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(IncludeFriends), false, "Friends are affected by avatar filtering");
            AbideByShowAvatar = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AbideByShowAvatar), true, "\"Show avatar\" bypasses avatar filtering");

            AvatarFilteringOnlyInPublic = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AvatarFilteringOnlyInPublic), false, "Do avatar filtering only in public instances");
            IncludeFriendsInHides = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(IncludeFriendsInHides), false, "Friends are affected by avatar hiding");
            HidesAbideByShowAvatar = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(HidesAbideByShowAvatar), true, "\"Show avatar\" bypasses avatar hiding");

            AllowSpawnSounds = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AllowSpawnSounds), false, "Allow avatar spawn sounds");
            AllowGlobalSounds = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AllowGlobalSounds), false, "Allow global sounds on avatars");
            MaxAudioSources = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxAudioSources), 16, "Max audio sources");

            MaxPolygons = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxPolygons), 2_000_000, "Max polygons");
            MaxMaterialSlots = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxMaterialSlots), 100, "Max material slots");
            HeuristicallyRemoveScreenSpaceBullshit = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(HeuristicallyRemoveScreenSpaceBullshit), true, "Try to remove fullscreen effects");
            
            MaxConstraints = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxConstraints), 200, "Max constraints");
            MaxColliders = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxColliders), 32, "Max colliders");
            MaxRigidBodies = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxRigidBodies), 32, "Max rigidbodies");

            MaxClothVertices = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxClothVertices), 10_000, "Max cloth vertices");
            MaxTransforms = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxTransforms), 1000, "Max bones/transforms");
            MaxDepth = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxDepth), 50, "Max transforms depth");
            
            MaxAnimators = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxAnimators), 64, "Max animators");
            MaxLights = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxLights), 2, "Max lights");
            MaxComponents = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxComponents), 4_000, "Max total components");
            
            MaxParticles = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxParticles), 1_000_000, "Max total particles");
            MaxMeshParticleVertices = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxMeshParticleVertices), 1_000_000, "Max total mesh particle polygons");
            AllowUiLayer = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AllowUiLayer), false, "Allow UI layer on avatars");
            
            AllowCustomMixers = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AllowCustomMixers), false, "Allow custom audio mixers on avatars");
            AllowReadingMixers = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AllowReadingMixers), false, "Allow audio mixers in assetbundles");
            MaxMaterialSlotsOverSubmeshCount = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(MaxMaterialSlotsOverSubmeshCount), 2, "Maximum material slots over submesh count");
            
            HidePortalsFromBlockedUsers = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(HidePortalsFromBlockedUsers), true, "Hide portals from blocked users");
            HidePortalsFromNonFriends = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(HidePortalsFromNonFriends), false, "Hide portals from non-friends");
            HidePortalsCreatedTooClose = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(HidePortalsCreatedTooClose), true, "Hide portals created too close to local player");
        }

        public static MelonPreferences_Entry<bool> AvatarFilteringEnabled;
        public static MelonPreferences_Entry<bool> AvatarFilteringOnlyInPublic;
        
        public static MelonPreferences_Entry<bool> AllowSpawnSounds;
        public static MelonPreferences_Entry<bool> AllowGlobalSounds;
        
        public static MelonPreferences_Entry<bool> IncludeFriends;
        public static MelonPreferences_Entry<bool> IncludeFriendsInHides;
        
        public static MelonPreferences_Entry<bool> AbideByShowAvatar;
        public static MelonPreferences_Entry<bool> HidesAbideByShowAvatar;
        
        public static MelonPreferences_Entry<bool> AllowUiLayer;
        public static MelonPreferences_Entry<bool> AllowCustomMixers;
        public static MelonPreferences_Entry<bool> AllowReadingMixers;

        public static MelonPreferences_Entry<int> MaxPolygons;
        public static MelonPreferences_Entry<int> MaxTransforms;
        public static MelonPreferences_Entry<int> MaxConstraints;
        public static MelonPreferences_Entry<int> MaxMaterialSlots;
        public static MelonPreferences_Entry<int> MaxAudioSources;
        public static MelonPreferences_Entry<int> MaxClothVertices;
        public static MelonPreferences_Entry<int> MaxColliders;
        public static MelonPreferences_Entry<int> MaxRigidBodies;
        public static MelonPreferences_Entry<int> MaxAnimators;
        public static MelonPreferences_Entry<int> MaxLights;
        public static MelonPreferences_Entry<int> MaxComponents;
        public static MelonPreferences_Entry<int> MaxDepth;
        public static MelonPreferences_Entry<int> MaxParticles;
        public static MelonPreferences_Entry<int> MaxMeshParticleVertices;
        public static MelonPreferences_Entry<int> MaxMaterialSlotsOverSubmeshCount;

        public static MelonPreferences_Entry<bool> HeuristicallyRemoveScreenSpaceBullshit;

        public static MelonPreferences_Entry<bool> HidePortalsFromBlockedUsers;
        public static MelonPreferences_Entry<bool> HidePortalsFromNonFriends;
        public static MelonPreferences_Entry<bool> HidePortalsCreatedTooClose;
    }
}