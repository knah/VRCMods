using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AdvancedSafety;
using MelonLoader;
using UIExpansionKit;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Core;
using AMEnumA = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObVRAc1GaApAcObBoUnique;
using AMEnumB = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObGaVRApBoBoObBoObUnique;
using Object = UnityEngine.Object;

[assembly:MelonModGame("VRChat", "VRChat")]
[assembly:MelonModInfo(typeof(AdvancedSafetyMod), "Advanced Safety", "1.1.2", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace AdvancedSafety
{
    public class AdvancedSafetyMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            AdvancedSafetySettings.RegisterSettings();

            XrefScanMethodDb.RegisterType<AssetManagement>();
            var matchingMethods = typeof(AssetManagement)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(it =>
                    it.Name.StartsWith("Method_Public_Static_Object_Object_Vector3_Quaternion_Boolean_Boolean_Boolean_") && it.GetParameters().Length == 6).ToList();

            MethodBase patchTarget = null;
            foreach (var methodInfo in typeof(AssetManagement).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (var it in XrefScanner.XrefScan(methodInfo))
                {
                    if (it.Type != XrefType.Method) continue;
                    var methodBase = it.TryResolve();
                    if (methodBase == null) continue;
                    if (matchingMethods.Contains(methodBase))
                    {
                        patchTarget = methodBase;
                        goto haveTarget;
                    }
                }
            }
            
            haveTarget:
            if (patchTarget != null)
            {
                unsafe
                {
                    var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(patchTarget).GetValue(null);

                    Imports.Hook((IntPtr) (&originalMethodPointer),
                        typeof(AdvancedSafetyMod).GetMethod(nameof(ObjectInstantiatePatch),
                            BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                    ourOriginalInstantiate =
                        Marshal.GetDelegateForFunctionPointer<ObjectInstantiateDelegate>(originalMethodPointer);
                }
            }
            else
            {
                MelonLogger.LogError("Patch target for object instantiation not found, avatar filtering will not work");
            }
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumA).GetMethod(
                        nameof(AMEnumA.MoveNext)))
                    .GetValue(null);
                
                Imports.Hook((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(MoveNextPatchA), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourMoveNextA = originalMethodPointer;
            }

            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AMEnumB).GetMethod(
                        nameof(AMEnumB.MoveNext)))
                    .GetValue(null);
                
                Imports.Hook((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(MoveNextPatchB), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourMoveNextB = originalMethodPointer;
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
            
            PortalHiding.OnApplicationStart();
            AvatarHiding.OnApplicationStart();
            
            if(Main.Mods.Any(it => it.InfoAttribute.SystemType.Name == nameof(UiExpansionKitMod)))
            {
                typeof(UiExpansionKitSupport).GetMethod(nameof(UiExpansionKitSupport.OnApplicationStart), BindingFlags.Static | BindingFlags.Public)!.Invoke(null, new object[0]);
            }
        }

        public override void OnModSettingsApplied()
        {
            AdvancedSafetySettings.OnModSettingsApplied();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ObjectInstantiateDelegate(IntPtr assetPtr, Vector3 pos, Quaternion rot, byte allowCustomShaders, byte isUI, byte validate);

        private static ObjectInstantiateDelegate ourOriginalInstantiate;
        private static IntPtr ourMoveNextA;
        private static IntPtr ourMoveNextB;

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
                RoomManagerBase.field_Internal_Static_ApiWorldInstance_0?.InstanceType != ApiWorldInstance.AccessType.Public)
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

            var animator = go.GetComponent<Animator>();

            var componentList = new Il2CppSystem.Collections.Generic.List<Component>();
            var audioSourcesList = new List<AudioSource>();
            
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

                if (!AdvancedSafetySettings.AllowUiLayer && (obj.layer == 12 || obj.layer == 5))
                    obj.layer = 9;

                obj.GetComponents(componentList);
                foreach (var component in componentList)
                {
                    if(component == null) continue;

                    component.TryCast<AudioSource>()?.VisitAudioSource(ref scannedObjects, ref destroyedObjects, ref seenAudioSources, obj, audioSourcesList);
                    component.TryCast<IConstraint>()?.VisitConstraint(ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    component.TryCast<Cloth>()?.VisitCloth(ref scannedObjects, ref destroyedObjects, ref seenClothVertices, obj);
                    component.TryCast<Rigidbody>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenRigidbodies, AdvancedSafetySettings.MaxRigidBodies);
                    
                    component.TryCast<Collider>()?.VisitCollider(ref scannedObjects, ref destroyedObjects, ref seenColliders, obj);
                    component.TryCast<Animator>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenAnimators, AdvancedSafetySettings.MaxAnimators);
                    component.TryCast<Light>()?.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenLights, AdvancedSafetySettings.MaxLights);
                    
                    component.TryCast<Renderer>()?.VisitRenderer(ref scannedObjects, ref destroyedObjects, ref seenPolys, ref seenMaterials, obj);
                    
                    if (ReferenceEquals(component.TryCast<Transform>(), null))
                        component.VisitGeneric(ref scannedObjects, ref destroyedObjects, ref seenComponents, AdvancedSafetySettings.MaxComponents);
                }
                
                foreach (var child in obj.transform) 
                    ourBfsQueue.Enqueue(new GameObjectWithPriorityData(child.Cast<Transform>().gameObject, objWithPriority.Depth + 1));
            }
            
            Bfs(new GameObjectWithPriorityData(go, 0));;
            while (ourBfsQueue.Count > 0) 
                Bfs(ourBfsQueue.Dequeue());

            if (!AdvancedSafetySettings.AllowSpawnSounds)
                MelonCoroutines.Start(CheckSpawnSounds(go, audioSourcesList));

            if (Imports.IsDebugMode() || destroyedObjects > 100)
                MelonLogger.Log($"Cleaned avatar ({avatarManager.prop_ApiAvatar_0?.name}) in {start.ElapsedMilliseconds}ms, scanned {scannedObjects} things, destroyed {destroyedObjects} things");
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
            catch (Exception ex)
            {
                MelonLogger.LogError($"Error when wrapping avatar creation: {ex}");
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
            catch (Exception ex)
            {
                MelonLogger.LogError($"Error when wrapping avatar creation: {ex}");
                return false;
            }
        }

        private static IntPtr ObjectInstantiatePatch(IntPtr assetPtr, Vector3 pos, Quaternion rot, byte allowCustomShaders, byte isUI, byte validate)
        {
            if (AvatarManagerCookie.CurrentManager == null || assetPtr == IntPtr.Zero) 
                return ourOriginalInstantiate(assetPtr, pos, rot, allowCustomShaders, isUI, validate);

            var avatarManager = AvatarManagerCookie.CurrentManager;
            var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
            if (vrcPlayer == null) return ourOriginalInstantiate(assetPtr, pos, rot, allowCustomShaders, isUI, validate);

            if (vrcPlayer == VRCPlayer.field_Internal_Static_VRCPlayer_0) // never apply to self
                return ourOriginalInstantiate(assetPtr, pos, rot, allowCustomShaders, isUI, validate);

            var go = new Object(assetPtr).TryCast<GameObject>();
            if (go == null)
                return ourOriginalInstantiate(assetPtr, pos, rot, allowCustomShaders, isUI, validate);

            var wasActive = go.activeSelf;
            go.SetActive(false);
            var result = ourOriginalInstantiate(assetPtr, pos, rot, allowCustomShaders, isUI, validate);
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
            foreach (var playerModeration in ModerationManager.prop_ModerationManager_0.field_Private_List_1_ApiPlayerModeration_0)
            {
                if (playerModeration.moderationType == ApiPlayerModeration.ModerationType.ShowAvatar && playerModeration.targetUserId == userId)
                    return true;
            }
            
            foreach (var playerModeration in ModerationManager.prop_ModerationManager_0.field_Private_List_1_ApiPlayerModeration_1)
            {
                if (playerModeration.moderationType == ApiPlayerModeration.ModerationType.ShowAvatar && playerModeration.targetUserId == userId)
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
            public readonly int NumChildren;
            public readonly int Depth;

            public GameObjectWithPriorityData(GameObject go, int depth)
            {
                GameObject = go;
                Depth = depth;
                IsActive = go.activeSelf;
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