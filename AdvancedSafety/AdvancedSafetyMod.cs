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
using UnityEngine;
using UnityEngine.Animations;
using VRC.Core;
using Object = UnityEngine.Object;

using AMEnumA = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObVRAc1GaApAcObBoUnique;
using AMEnumB = VRCAvatarManager.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObGaVRApBoBoObBoObUnique;

[assembly:MelonModGame("VRChat", "VRChat")]
[assembly:MelonModInfo(typeof(AdvancedSafetyMod), "Advanced Safety", "1.0.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace AdvancedSafety
{
    public class AdvancedSafetyMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            AdvancedSafetySettings.RegisterSettings();

            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(AssetManagement).GetMethod(
                        nameof(AssetManagement.Method_Public_Static_Object_Object_Vector3_Quaternion_Boolean_Boolean_Boolean_0)))
                    .GetValue(null);
                
                Imports.Hook((IntPtr)(&originalMethodPointer), typeof(AdvancedSafetyMod).GetMethod(nameof(ObjectInstantiatePatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourOriginalInstantiate = Marshal.GetDelegateForFunctionPointer<ObjectInstantiateDelegate>(originalMethodPointer);
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
        private delegate IntPtr ObjectInstantiateDelegate(IntPtr assetPtr, Vector3 pos, Quaternion rot, bool allowCustomShaders, bool isUI, bool validate);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool MoveNextDelegate(IntPtr thisPtr);

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

        private static readonly Queue<GameObject> ourBfsQueue = new Queue<GameObject>();
        public static void CleanAvatar(VRCAvatarManager avatarManager, GameObject go)
        {
            if (!AdvancedSafetySettings.AvatarFilteringEnabled) 
                return;
            
            if (AdvancedSafetySettings.AvatarFilteringOnlyInPublic &&
                RoomManagerBase.field_Internal_Static_ApiWorldInstance_0?.InstanceType != ApiWorldInstance.AccessType.Public)
                return;
            
            var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
            if (vrcPlayer == null) return;
            
            if (vrcPlayer == VRCPlayer.field_Internal_Static_VRCPlayer_0) // never apply to self
                return;
            
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

            var animator = go.GetComponent<Animator>();

            var componentList = new Il2CppSystem.Collections.Generic.List<Component>();
            var audioSourcesList = new List<AudioSource>();
            
            void Bfs(GameObject obj)
            {
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

                    ComponentAdjustment.VisitAudioSource(component.TryCast<AudioSource>(), ref scannedObjects, ref destroyedObjects, ref seenAudioSources, obj, audioSourcesList);
                    ComponentAdjustment.VisitConstraint(component.TryCast<IConstraint>(), ref scannedObjects, ref destroyedObjects, ref seenConstraints, obj);
                    ComponentAdjustment.VisitCloth(component.TryCast<Cloth>(), ref scannedObjects, ref destroyedObjects, ref seenClothVertices, obj);
                    ComponentAdjustment.VisitCollider(component.TryCast<Collider>(), ref scannedObjects, ref destroyedObjects, ref seenColliders, obj);
                    
                    ComponentAdjustment.VisitGeneric(component.TryCast<Rigidbody>(), ref scannedObjects, ref destroyedObjects, ref seenRigidbodies, AdvancedSafetySettings.MaxRigidBodies);
                    ComponentAdjustment.VisitGeneric(component.TryCast<Animator>(), ref scannedObjects, ref destroyedObjects, ref seenAnimators, AdvancedSafetySettings.MaxAnimators);
                    ComponentAdjustment.VisitGeneric(component.TryCast<Light>(), ref scannedObjects, ref destroyedObjects, ref seenLights, AdvancedSafetySettings.MaxLights);
                    
                    ComponentAdjustment.VisitRenderer(component.TryCast<Renderer>(), ref scannedObjects, ref destroyedObjects, ref seenPolys, ref seenMaterials, obj);
                }
                
                foreach (var child in obj.transform) 
                    ourBfsQueue.Enqueue(child.Cast<Transform>().gameObject);
            }
            
            Bfs(go);
            while (ourBfsQueue.Count > 0) 
                Bfs(ourBfsQueue.Dequeue());

            if (!AdvancedSafetySettings.AllowSpawnSounds)
                MelonCoroutines.Start(CheckSpawnSounds(go, audioSourcesList));

            MelonModLogger.Log($"Cleaned avatar ({avatarManager.prop_ApiAvatar_0?.name}) in {start.ElapsedMilliseconds}ms, scanned {scannedObjects} things, destroyed {destroyedObjects} things");
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
                MelonModLogger.LogError($"Error when wrapping avatar creation: {ex}");
                return false;
            }
        }
        
        private static bool MoveNextPatchB(IntPtr thisPtr)
        {
            try
            {
                using (new AvatarManagerCookie(new AMEnumB(thisPtr).field_Public_VRCAvatarManager_0))
                    return SafeInvokeMoveNext(ourMoveNextA, thisPtr);
            }
            catch (Exception ex)
            {
                MelonModLogger.LogError($"Error when wrapping avatar creation: {ex}");
                return false;
            }
        }

        private static IntPtr ObjectInstantiatePatch(IntPtr assetPtr, Vector3 pos, Quaternion rot, bool allowCustomShaders, bool isUI, bool validate)
        {
            if (AvatarManagerCookie.CurrentManager == null || assetPtr == IntPtr.Zero) 
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
                MelonModLogger.LogError($"Exception when cleaning avatar: {ex}");
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
    }
}