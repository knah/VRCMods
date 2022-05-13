using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AdvancedSafety;
using AdvancedSafety.BundleVerifier;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.Management;
using Object = UnityEngine.Object;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(AdvancedSafetyMod), "Advanced Safety", "1.6.1", "knah, Requi, Ben", "https://github.com/knah/VRCMods")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace AdvancedSafety
{
    internal partial class AdvancedSafetyMod : MelonMod
    {
        internal static bool CanReadAudioMixers = true;
        internal static bool CanReadBadFloats = true;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidDelegate(IntPtr thisPtr, IntPtr nativeMethodInfo);

        public override void OnApplicationStart()
        {
            if (!CheckWasSuccessful || !MustStayTrue || MustStayFalse) return;
            
            AdvancedSafetySettings.RegisterSettings();
            ClassInjector.RegisterTypeInIl2Cpp<SortingOrderHammerer>();

            try
            {
                BundleVerifierMod.OnApplicationStart(HarmonyInstance);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error initializing Bundle Verifier: {ex}");
            }

            var matchingMethods = typeof(AssetManagement)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(it =>
                    it.Name.StartsWith("Method_Public_Static_Object_Object_Vector3_Quaternion_Boolean_Boolean_Boolean_") && it.GetParameters().Length == 6).ToList();

            foreach (var matchingMethod in matchingMethods)
            {
                ObjectInstantiateDelegate originalInstantiateDelegate = null;

                ObjectInstantiateDelegate replacement = (assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer) =>
                    ObjectInstantiatePatch(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer, originalInstantiateDelegate);

                NativePatchUtils.NativePatch(matchingMethod, out originalInstantiateDelegate, replacement);
            }
            
            foreach (var nestedType in typeof(VRCAvatarManager).GetNestedTypes())
            {
                var moveNext = nestedType.GetMethod("MoveNext");
                if (moveNext == null) continue;
                var avatarManagerField = nestedType.GetProperties().SingleOrDefault(it => it.PropertyType == typeof(VRCAvatarManager));
                if (avatarManagerField == null) continue;
                
                MelonDebug.Msg($"Patching UniTask type {nestedType.FullName}");

                var fieldOffset = (int)IL2CPP.il2cpp_field_get_offset((IntPtr)UnhollowerUtils
                    .GetIl2CppFieldInfoPointerFieldForGeneratedFieldAccessor(avatarManagerField.GetMethod)
                    .GetValue(null));

                unsafe
                {
                    var originalMethodPointer = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(moveNext).GetValue(null);

                    originalMethodPointer = XrefScannerLowLevel.JumpTargets(originalMethodPointer).First();

                    VoidDelegate originalDelegate = null;
                    
                    void TaskMoveNextPatch(IntPtr taskPtr, IntPtr nativeMethodInfo)
                    {
                        var avatarManager = *(IntPtr*)(taskPtr + fieldOffset - 16);
                        using (new AvatarManagerCookie(new VRCAvatarManager(avatarManager)))
                            originalDelegate(taskPtr, nativeMethodInfo);
                    }
                    
                    var patchDelegate = new VoidDelegate(TaskMoveNextPatch);
                    
                    NativePatchUtils.NativePatch(originalMethodPointer, out originalDelegate, patchDelegate);
                }
            }

            ReaderPatches.ApplyPatches();
            
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((s, _) =>
            {
                if (s.buildIndex == -1)
                {
                    CanReadAudioMixers = false;
                    CanReadBadFloats = false;
                    MelonDebug.Msg("No reading audio mixers anymore");
                }
            }));
            
            SceneManager.add_sceneUnloaded(new Action<Scene>(s =>
            {
                if (s.buildIndex == -1)
                {
                    // allow loading mixers from world assetbundles 
                    CanReadAudioMixers = true;
                    CanReadBadFloats = true;
                    MelonDebug.Msg("Can read audio mixers now");
                }
            }));
            
            PortalHiding.OnApplicationStart();
            AvatarHiding.OnApplicationStart(HarmonyInstance);
            FinalIkPatches.ApplyPatches(HarmonyInstance);
            
            if(MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit"))
            {
                typeof(UiExpansionKitSupport).GetMethod(nameof(UiExpansionKitSupport.OnApplicationStart), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[0]);
            }
        }

        public override void OnApplicationQuit()
        {
            BundleVerifierMod.OnApplicationQuit();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ObjectInstantiateDelegate(IntPtr assetPtr, Vector3 pos, Quaternion rot, byte allowCustomShaders, byte isUI, byte validate, IntPtr nativeMethodPointer);

        private static readonly PriorityQueue<GameObjectWithPriorityData> ourBfsQueue = new PriorityQueue<GameObjectWithPriorityData>(GameObjectWithPriorityData.IsActiveDepthNumChildrenComparer);
        private static void CleanAvatar(VRCAvatarManager avatarManager, GameObject go)
        {
            if (!AdvancedSafetySettings.AvatarFilteringEnabled.Value) 
                return;
            
            if (AdvancedSafetySettings.AvatarFilteringOnlyInPublic.Value &&
                RoomManager.field_Internal_Static_ApiWorldInstance_0?.type != InstanceAccessType.Public)
                return;
            
            var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
            if (vrcPlayer == null) return;
            
            var userId = vrcPlayer.prop_Player_0?.prop_APIUser_0?.id ?? "";
            if (!AdvancedSafetySettings.IncludeFriends.Value && APIUser.IsFriendsWith(userId))
                return;

            if (AdvancedSafetySettings.AbideByShowAvatar.Value && IsAvatarExplicitlyShown(userId))
                return;

            var start = Stopwatch.StartNew();
            var scannedObjects = 0;
            var destroyedObjects = 0;

            var seenTransforms = 0;
            var seenPolys = 0;
            var seenMaterials = 0;
            var seenAudioSources = 0;
            var seenConstraints = 0;
            var seenClothVertices = 0;
            var seenColliders = 0;
            var seenRigidbodies = 0;
            var seenAnimators = 0;
            var seenLights = 0;
            var seenComponents = 0;
            var seenParticles = 0;
            var seenMeshParticleVertices = 0;
            var trailLimit = AdvancedSafetySettings.MaxParticleTrails.Value;

            var animator = go.GetComponent<Animator>();

            var componentList = new Il2CppSystem.Collections.Generic.List<Component>();
            var audioSourcesList = new List<AudioSource>();
            var skinnedRendererListList = new List<SkinnedMeshRenderer>();
            
            void Bfs(GameObjectWithPriorityData objWithPriority)
            {
                var obj = objWithPriority.GameObject;
                
                if (obj == null) return;
                scannedObjects++;

                if (animator?.IsBoneTransform(obj.transform) != true && seenTransforms++ >= AdvancedSafetySettings.MaxTransforms.Value)
                {
                    Object.DestroyImmediate(obj, true);
                    destroyedObjects++;
                    return;
                }

                if (objWithPriority.Depth >= AdvancedSafetySettings.MaxDepth.Value)
                {
                    Object.DestroyImmediate(obj, true);
                    destroyedObjects++;
                    return;
                }

                if (!AdvancedSafetySettings.AllowUiLayer.Value && (obj.layer == 12 || obj.layer == 5))
                    obj.layer = 9;

                obj.GetComponents(componentList);
                foreach (var component in componentList)
                {
                    if (component == null) continue;

                    component.TryCast<AudioSource>()?.VisitAudioSource(ref scannedObjects, ref destroyedObjects, ref seenAudioSources, obj, audioSourcesList, objWithPriority.IsActiveInHierarchy);
                    
                    component.TryCast<ParentConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<RotationConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<PositionConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<ScaleConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<LookAtConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<AimConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    
                    component.TryCast<Cloth>()?.VisitCloth(ref scannedObjects, ref destroyedObjects, ref seenClothVertices, obj);
                    component.TryCast<Rigidbody>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenRigidbodies, AdvancedSafetySettings.MaxRigidBodies.Value);
                    
                    component.TryCast<Collider>()?.VisitCollider(ref scannedObjects, ref destroyedObjects, ref seenColliders, obj);
                    component.TryCast<Animator>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenAnimators, AdvancedSafetySettings.MaxAnimators.Value);
                    component.TryCast<Light>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenLights, AdvancedSafetySettings.MaxLights.Value);
                    
                    component.TryCast<Renderer>()?.VisitRenderer(ref scannedObjects, ref destroyedObjects, ref seenPolys, ref seenMaterials, obj, skinnedRendererListList);
                    component.TryCast<ParticleSystem>()?.VisitParticleSystem(component.GetComponent<ParticleSystemRenderer>(), ref scannedObjects, ref destroyedObjects, ref seenParticles, ref seenMeshParticleVertices, ref trailLimit, obj);
                    
                    if (ReferenceEquals(component.TryCast<Transform>(), null))
                        component.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenComponents, AdvancedSafetySettings.MaxComponents.Value);
                }
                
                foreach (var child in obj.transform) 
                    ourBfsQueue.Enqueue(new GameObjectWithPriorityData(child.Cast<Transform>().gameObject, objWithPriority.Depth + 1, objWithPriority.IsActiveInHierarchy));
            }
            
            Bfs(new GameObjectWithPriorityData(go, 0, true, true));
            while (ourBfsQueue.Count > 0) 
                Bfs(ourBfsQueue.Dequeue());
            
            ComponentAdjustment.PostprocessSkinnedRenderers(skinnedRendererListList);

            if (!AdvancedSafetySettings.AllowSpawnSounds.Value)
                MelonCoroutines.Start(CheckSpawnSounds(go, audioSourcesList));

            if (AdvancedSafetySettings.EnforceDefaultSortingLayer.Value)
                go.AddComponent<SortingOrderHammerer>();

            if (MelonDebug.IsEnabled() || destroyedObjects > 100)
                MelonLogger.Msg($"Cleaned avatar ({avatarManager.field_Private_ApiAvatar_0?.name}) used by \"{vrcPlayer.prop_VRCPlayerApi_0?.displayName}\" in {start.ElapsedMilliseconds}ms, scanned {scannedObjects} things, destroyed {destroyedObjects} things");
        }

        private static IEnumerator CheckSpawnSounds(GameObject go, List<AudioSource> audioSourcesList)
        {
            if (audioSourcesList.Count == 0)
                yield break;
            
            var endTime = Time.time + 5f;
            while (go != null && !go.activeInHierarchy && Time.time < endTime)
                yield return null;

            yield return null;
            yield return null;

            if (go == null || !go.activeInHierarchy)
                yield break;
            
            foreach (var audioSource in audioSourcesList)
                if (audioSource != null && audioSource.isPlaying)
                    audioSource.Stop();
        }

        private static IntPtr ObjectInstantiatePatch(IntPtr assetPtr, Vector3 pos, Quaternion rot,
            byte allowCustomShaders, byte isUI, byte validate, IntPtr nativeMethodPointer, ObjectInstantiateDelegate originalInstantiateDelegate)
        {
            if (AvatarManagerCookie.CurrentManager == null || assetPtr == IntPtr.Zero)
                return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);

            var avatarManager = AvatarManagerCookie.CurrentManager;
            var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
            if (vrcPlayer == null) return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);

            if (vrcPlayer == VRCPlayer.field_Internal_Static_VRCPlayer_0) // never apply to self
                return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);

            var go = new Object(assetPtr).TryCast<GameObject>();
            if (go == null)
                return originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);

            var wasActive = go.activeSelf;
            go.SetActive(false);
            var result = originalInstantiateDelegate(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer);
            go.SetActive(wasActive);
            if (result == IntPtr.Zero) return result;
            var instantiated = new GameObject(result);
            try
            {
                CleanAvatar(AvatarManagerCookie.CurrentManager, instantiated);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception when cleaning avatar: {ex}");
            }
            
            return result;
        }

        internal static bool IsAvatarExplicitlyShown(string userId)
        {
            var moderationsDict = ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0;
            if (!moderationsDict.ContainsKey(userId)) return false;
            
            foreach (var playerModeration in moderationsDict[userId])
            {
                if (playerModeration.moderationType == ApiPlayerModeration.ModerationType.ShowAvatar)
                    return true;
            }
            
            return false;
        }

        private readonly struct AvatarManagerCookie : IDisposable
        {
            internal static VRCAvatarManager CurrentManager;
            private readonly VRCAvatarManager myLastManager;

            public AvatarManagerCookie(VRCAvatarManager avatarManager)
            {
                myLastManager = CurrentManager;
                CurrentManager = avatarManager;
            }
            public void Dispose()
            {
                CurrentManager = myLastManager;
            }
        }

        private readonly struct GameObjectWithPriorityData
        {
            public readonly GameObject GameObject;
            public readonly bool IsActive;
            public readonly bool IsActiveInHierarchy;
            public readonly int NumChildren;
            public readonly int Depth;

            public GameObjectWithPriorityData(GameObject go, int depth, bool parentActive, bool enforceActive = false)
            {
                GameObject = go;
                Depth = depth;
                IsActive = go.activeSelf || enforceActive;
                IsActiveInHierarchy = IsActive && parentActive;
                NumChildren = go.transform.childCount;
            }

            public int Priority => Depth + NumChildren;

            private sealed class IsActiveDepthNumChildrenRelationalComparer : IComparer<GameObjectWithPriorityData>
            {
                public int Compare(GameObjectWithPriorityData x, GameObjectWithPriorityData y)
                {
                    var isActiveComparison = -x.IsActive.CompareTo(y.IsActive);
                    if (isActiveComparison != 0) return isActiveComparison;
                    return x.Priority.CompareTo(y.Priority);
                }
            }

            public static IComparer<GameObjectWithPriorityData> IsActiveDepthNumChildrenComparer { get; } = new IsActiveDepthNumChildrenRelationalComparer();
        }
    }
}