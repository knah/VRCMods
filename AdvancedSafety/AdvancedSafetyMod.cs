using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AdvancedSafety;
using Harmony;
using MelonLoader;
using UIExpansionKit;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using VRC.Core;
using AMEnumA = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObAcOb1GaApAcBoStUnique;
using AMEnumB = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObAcOb1GaApAcObObUnique;
using AMEnumC = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObGaApObBoBoBoObObUnique;
using Object = UnityEngine.Object;

using ModerationManager = VRC.Management.ModerationManager;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(AdvancedSafetyMod), "Advanced Safety", "1.5.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace AdvancedSafety
{
    public class AdvancedSafetyMod : MelonMod
    {
        private static List<object> ourPinnedDelegates = new List<object>();
        private static bool ourCanReadAudioMixers = true;

        public override void OnApplicationStart()
        {
            AdvancedSafetySettings.RegisterSettings();

            var matchingMethods = typeof(AssetManagement)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(it =>
                    it.Name.StartsWith("Method_Public_Static_Object_Object_Vector3_Quaternion_Boolean_Boolean_Boolean_") && it.GetParameters().Length == 6).ToList();

            foreach (var matchingMethod in matchingMethods)
            {
                unsafe
                {
                    var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(matchingMethod).GetValue(null);

                    ObjectInstantiateDelegate originalInstantiateDelegate = null;

                    ObjectInstantiateDelegate replacement = (assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer) =>
                        ObjectInstantiatePatch(assetPtr, pos, rot, allowCustomShaders, isUI, validate, nativeMethodPointer, originalInstantiateDelegate);

                    ourPinnedDelegates.Add(replacement);

                    MelonUtils.NativeHookAttach((IntPtr) (&originalMethodPointer), Marshal.GetFunctionPointerForDelegate(replacement));

                    originalInstantiateDelegate = Marshal.GetDelegateForFunctionPointer<ObjectInstantiateDelegate>(originalMethodPointer);
                }
            }
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumA).GetMethod(
                        nameof(AMEnumA.MoveNext)))
                    .GetValue(null);
                
                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(MoveNextPatchA), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourMoveNextA = originalMethodPointer;
            }

            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumB).GetMethod(
                        nameof(AMEnumB.MoveNext)))
                    .GetValue(null);
                
                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(MoveNextPatchB), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourMoveNextB = originalMethodPointer;
            }
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumC).GetMethod(
                        nameof(AMEnumC.MoveNext)))
                    .GetValue(null);
                
                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(MoveNextPatchC), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourMoveNextC = originalMethodPointer;
            }

            unsafe
            {
                var originalMethodInfo = (Il2CppMethodInfo*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumB).GetMethod(
                        nameof(AMEnumB.MoveNext)))
                    .GetValue(null);

                var methodInfoCopy = (Il2CppMethodInfo*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfo>());
                *methodInfoCopy = *originalMethodInfo;

                ourInvokeMethodInfo = (IntPtr) methodInfoCopy;
            }
            
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (!module.FileName.Contains("UnityPlayer")) continue;

                // ProduceHelper<AudioMixer,0>::Produce, thanks to Ben for finding an adjacent method
                ourAudioMixerReadPointer = module.BaseAddress + 0x4997C0;
                var patchDelegate = new AudioMixerReadDelegate(AudioMixerReadPatch);
                ourPinnedDelegates.Add(patchDelegate);
                unsafe
                {
                    fixed (IntPtr* mixerReadAddress = &ourAudioMixerReadPointer)
                        MelonUtils.NativeHookAttach((IntPtr) mixerReadAddress, Marshal.GetFunctionPointerForDelegate(patchDelegate));
                    ourAudioMixerReadDelegate = Marshal.GetDelegateForFunctionPointer<AudioMixerReadDelegate>(ourAudioMixerReadPointer);
                }
                    
                break;
            }
            
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((s, _) =>
            {
                if (s.buildIndex == -1)
                {
                    ourCanReadAudioMixers = false;
                    MelonDebug.Msg("No reading audio mixers anymore");
                }
            }));
            
            SceneManager.add_sceneUnloaded(new Action<Scene>(s =>
            {
                if (s.buildIndex == -1)
                {
                    // allow loading mixers from world assetbundles 
                    ourCanReadAudioMixers = true;
                    MelonDebug.Msg("Can read audio mixers now");
                }
            }));
            
            PortalHiding.OnApplicationStart();
            AvatarHiding.OnApplicationStart(Harmony);
            
            if(MelonHandler.Mods.Any(it => it.Info.SystemType.Name == nameof(UiExpansionKitMod)))
            {
                typeof(UiExpansionKitSupport).GetMethod(nameof(UiExpansionKitSupport.OnApplicationStart), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[0]);
            }
        }

        public override void OnModSettingsApplied()
        {
            AdvancedSafetySettings.OnModSettingsApplied();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ObjectInstantiateDelegate(IntPtr assetPtr, Vector3 pos, Quaternion rot, byte allowCustomShaders, byte isUI, byte validate, IntPtr nativeMethodPointer);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr AudioMixerReadDelegate(int thisPtr, int readerPtr);

        private static AudioMixerReadDelegate ourAudioMixerReadDelegate;
        private static IntPtr ourAudioMixerReadPointer;

        private static IntPtr ourMoveNextA;
        private static IntPtr ourMoveNextB;
        private static IntPtr ourMoveNextC;

        private static IntPtr ourInvokeMethodInfo;

        private unsafe static bool SafeInvokeMoveNext(IntPtr methodPtr, IntPtr thisPtr)
        {
            var exc = IntPtr.Zero;
            ((Il2CppMethodInfo*) ourInvokeMethodInfo)->methodPointer = methodPtr;
            var result = IL2CPP.il2cpp_runtime_invoke(ourInvokeMethodInfo, thisPtr, (void**) IntPtr.Zero, ref exc);
            Il2CppException.RaiseExceptionIfNecessary(exc);
            return * (bool*) IL2CPP.il2cpp_object_unbox(result);
        }

        private static readonly PriorityQueue<GameObjectWithPriorityData> ourBfsQueue = new PriorityQueue<GameObjectWithPriorityData>(GameObjectWithPriorityData.IsActiveDepthNumChildrenComparer);
        private static void CleanAvatar(VRCAvatarManager avatarManager, GameObject go)
        {
            if (!AdvancedSafetySettings.AvatarFilteringEnabled) 
                return;
            
            if (AdvancedSafetySettings.AvatarFilteringOnlyInPublic &&
                RoomManager.field_Internal_Static_ApiWorldInstance_0?.InstanceType != ApiWorldInstance.AccessType.Public)
                return;
            
            var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
            if (vrcPlayer == null) return;
            
            var userId = vrcPlayer.prop_Player_0?.prop_APIUser_0?.id ?? "";
            if (!AdvancedSafetySettings.IncludeFriends && APIUser.IsFriendsWith(userId))
                return;

            if (AdvancedSafetySettings.AbideByShowAvatar && IsAvatarExplicitlyShown(userId))
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

            var animator = go.GetComponent<Animator>();

            var componentList = new Il2CppSystem.Collections.Generic.List<Component>();
            var audioSourcesList = new List<AudioSource>();
            var skinnedRendererListList = new List<SkinnedMeshRenderer>();
            
            void Bfs(GameObjectWithPriorityData objWithPriority)
            {
                var obj = objWithPriority.GameObject;
                
                if (obj == null) return;
                scannedObjects++;

                if (animator?.IsBoneTransform(obj.transform) != true && seenTransforms++ >= AdvancedSafetySettings.MaxTransforms)
                {
                    Object.DestroyImmediate(obj, true);
                    destroyedObjects++;
                    return;
                }

                if (objWithPriority.Depth >= AdvancedSafetySettings.MaxDepth)
                {
                    Object.DestroyImmediate(obj, true);
                    destroyedObjects++;
                    return;
                }

                if (!AdvancedSafetySettings.AllowUiLayer && (obj.layer == 12 || obj.layer == 5))
                    obj.layer = 9;

                obj.GetComponents(componentList);
                foreach (var component in componentList)
                {
                    if (component == null) continue;

                    component.TryCast<AudioSource>()?.VisitAudioSource(ref scannedObjects, ref destroyedObjects, ref seenAudioSources, obj, audioSourcesList, objWithPriority.IsActiveInHierarchy);
                    component.TryCast<IConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<Cloth>()?.VisitCloth(ref scannedObjects, ref destroyedObjects, ref seenClothVertices, obj);
                    component.TryCast<Rigidbody>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenRigidbodies, AdvancedSafetySettings.MaxRigidBodies);
                    
                    component.TryCast<Collider>()?.VisitCollider(ref scannedObjects, ref destroyedObjects, ref seenColliders, obj);
                    component.TryCast<Animator>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenAnimators, AdvancedSafetySettings.MaxAnimators);
                    component.TryCast<Light>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenLights, AdvancedSafetySettings.MaxLights);
                    
                    component.TryCast<Renderer>()?.VisitRenderer(ref scannedObjects, ref destroyedObjects, ref seenPolys, ref seenMaterials, obj, skinnedRendererListList);
                    component.TryCast<ParticleSystem>()?.VisitParticleSystem(component.GetComponent<ParticleSystemRenderer>(), ref scannedObjects, ref destroyedObjects, ref seenParticles, ref seenMeshParticleVertices, obj);
                    
                    if (ReferenceEquals(component.TryCast<Transform>(), null))
                        component.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenComponents, AdvancedSafetySettings.MaxComponents);
                }
                
                foreach (var child in obj.transform) 
                    ourBfsQueue.Enqueue(new GameObjectWithPriorityData(child.Cast<Transform>().gameObject, objWithPriority.Depth + 1, objWithPriority.IsActiveInHierarchy));
            }
            
            Bfs(new GameObjectWithPriorityData(go, 0, true, true));
            while (ourBfsQueue.Count > 0) 
                Bfs(ourBfsQueue.Dequeue());
            
            ComponentAdjustment.PostprocessSkinnedRenderers(skinnedRendererListList);

            if (!AdvancedSafetySettings.AllowSpawnSounds)
                MelonCoroutines.Start(CheckSpawnSounds(go, audioSourcesList));

            if (MelonDebug.IsEnabled() || destroyedObjects > 100)
                MelonLogger.Msg($"Cleaned avatar ({avatarManager.prop_ApiAvatar_0?.name}) used by \"{vrcPlayer.prop_VRCPlayerApi_0?.displayName}\" in {start.ElapsedMilliseconds}ms, scanned {scannedObjects} things, destroyed {destroyedObjects} things");
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

        private static bool MoveNextPatchA(IntPtr thisPtr)
        {
            try
            {
                using (new AvatarManagerCookie(new AMEnumA(thisPtr).field_Public_VRCAvatarManager_0))
                    return SafeInvokeMoveNext(ourMoveNextA, thisPtr);
            }
            catch (Il2CppException ex)
            {
                MelonDebug.Msg($"Caught top-level native exception: {ex}");
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error when wrapping avatar creation: {ex}");
                return false;
            }
        }
        
        private static bool MoveNextPatchB(IntPtr thisPtr)
        {
            try
            {
                using (new AvatarManagerCookie(new AMEnumB(thisPtr).field_Public_VRCAvatarManager_0))
                    return SafeInvokeMoveNext(ourMoveNextB, thisPtr);
            }
            catch (Il2CppException ex)
            {
                MelonDebug.Msg($"Caught top-level native exception: {ex}");
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error when wrapping avatar creation: {ex}");
                return false;
            }
        }

        private static bool MoveNextPatchC(IntPtr thisPtr)
        {
            try
            {
                using (new AvatarManagerCookie(new AMEnumC(thisPtr).field_Public_VRCAvatarManager_0))
                    return SafeInvokeMoveNext(ourMoveNextC, thisPtr);
            }
            catch (Il2CppException ex)
            {
                MelonDebug.Msg($"Caught top-level native exception: {ex}");
                return false;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error when wrapping avatar creation: {ex}");
                return false;
            }
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
                MelonLogger.LogError($"Exception when cleaning avatar: {ex}");
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

        private static IntPtr AudioMixerReadPatch(int thisPtr, int readerPtr)
        {
            if (!ourCanReadAudioMixers && !AdvancedSafetySettings.AllowReadingMixers)
            {
                MelonDebug.Msg("Not reading audio mixer");
                return IntPtr.Zero;
            }
            
            // just in case something ever races
            ourAudioMixerReadDelegate ??= Marshal.GetDelegateForFunctionPointer<AudioMixerReadDelegate>(ourAudioMixerReadPointer);
            return ourAudioMixerReadDelegate(thisPtr, readerPtr);
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