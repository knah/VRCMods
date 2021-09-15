using MelonLoader;

namespace AdvancedSafety
{
    public static class AdvancedSafetySettings
    {
        private const string SettingsCategory = "AdvancedSafety";

        public static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory(SettingsCategory, "Advanced safety");
            
            AvatarFilteringEnabled = category.CreateEntry( nameof(AvatarFilteringEnabled), true, "Enable avatar filtering");
            IncludeFriends = category.CreateEntry(nameof(IncludeFriends), false, "Friends are affected by avatar filtering");
            AbideByShowAvatar = category.CreateEntry(nameof(AbideByShowAvatar), true, "\"Show avatar\" bypasses avatar filtering");

            AvatarFilteringOnlyInPublic = category.CreateEntry(nameof(AvatarFilteringOnlyInPublic), false, "Do avatar filtering only in public instances");
            IncludeFriendsInHides = category.CreateEntry(nameof(IncludeFriendsInHides), false, "Friends are affected by avatar hiding");
            HidesAbideByShowAvatar = category.CreateEntry(nameof(HidesAbideByShowAvatar), true, "\"Show avatar\" bypasses avatar hiding");

            AllowSpawnSounds = category.CreateEntry(nameof(AllowSpawnSounds), false, "Allow avatar spawn sounds");
            AllowGlobalSounds = category.CreateEntry(nameof(AllowGlobalSounds), false, "Allow global sounds on avatars");
            MaxAudioSources = category.CreateEntry(nameof(MaxAudioSources), 16, "Max audio sources");

            MaxPolygons = category.CreateEntry(nameof(MaxPolygons), 2_000_000, "Max polygons");
            MaxMaterialSlots = category.CreateEntry(nameof(MaxMaterialSlots), 100, "Max material slots");
            HeuristicallyRemoveScreenSpaceBullshit = category.CreateEntry(nameof(HeuristicallyRemoveScreenSpaceBullshit), true, "Try to remove fullscreen effects");
            
            MaxConstraints = category.CreateEntry(nameof(MaxConstraints), 200, "Max constraints");
            MaxColliders = category.CreateEntry(nameof(MaxColliders), 32, "Max colliders");
            MaxRigidBodies = category.CreateEntry(nameof(MaxRigidBodies), 32, "Max rigidbodies");

            MaxClothVertices = category.CreateEntry(nameof(MaxClothVertices), 10_000, "Max cloth vertices");
            MaxTransforms = category.CreateEntry(nameof(MaxTransforms), 1000, "Max bones/transforms");
            MaxDepth = category.CreateEntry(nameof(MaxDepth), 50, "Max transforms depth");
            
            MaxAnimators = category.CreateEntry(nameof(MaxAnimators), 64, "Max animators");
            MaxLights = category.CreateEntry(nameof(MaxLights), 2, "Max lights");
            MaxComponents = category.CreateEntry(nameof(MaxComponents), 4_000, "Max total components");
            
            MaxParticles = category.CreateEntry(nameof(MaxParticles), 1_000_000, "Max total particles");
            MaxMeshParticleVertices = category.CreateEntry(nameof(MaxMeshParticleVertices), 1_000_000, "Max total mesh particle polygons");
            MaxParticleTrails = category.CreateEntry(nameof(MaxParticleTrails), 64, "Maximum particle trails");
            
            AllowUiLayer = category.CreateEntry(nameof(AllowUiLayer), false, "Allow UI layer on avatars");
            AllowCustomMixers = category.CreateEntry(nameof(AllowCustomMixers), false, "Allow custom audio mixers on avatars");
            AllowReadingMixers = category.CreateEntry(nameof(AllowReadingMixers), false, "Allow audio mixers in assetbundles");
            
            MaxMaterialSlotsOverSubmeshCount = category.CreateEntry(nameof(MaxMaterialSlotsOverSubmeshCount), 2, "Maximum material slots over submesh count");
            AllowReadingBadFloats = category.CreateEntry(nameof(AllowReadingBadFloats), false, "Allow unbounded floats in assetbundles");
            AllowNonDefaultSortingLayers = category.CreateEntry(nameof(AllowNonDefaultSortingLayers), false, "Allow non-default sorting layers (overrender)");
            
            EnforceDefaultSortingLayer = category.CreateEntry(nameof(EnforceDefaultSortingLayer), true, "Enforce default sorting layer (less overrender, affects performance)");
            
            HidePortalsFromBlockedUsers = category.CreateEntry(nameof(HidePortalsFromBlockedUsers), true, "Hide portals from blocked users");
            HidePortalsFromNonFriends = category.CreateEntry(nameof(HidePortalsFromNonFriends), false, "Hide portals from non-friends");
            HidePortalsCreatedTooClose = category.CreateEntry(nameof(HidePortalsCreatedTooClose), true, "Hide portals created too close to local player");
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
        public static MelonPreferences_Entry<bool> AllowReadingBadFloats;
        public static MelonPreferences_Entry<bool> AllowNonDefaultSortingLayers;
        public static MelonPreferences_Entry<bool> EnforceDefaultSortingLayer;

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
        public static MelonPreferences_Entry<int> MaxParticleTrails;

        public static MelonPreferences_Entry<bool> HeuristicallyRemoveScreenSpaceBullshit;

        public static MelonPreferences_Entry<bool> HidePortalsFromBlockedUsers;
        public static MelonPreferences_Entry<bool> HidePortalsFromNonFriends;
        public static MelonPreferences_Entry<bool> HidePortalsCreatedTooClose;
    }
}