using System;
using System.Collections;
using System.Runtime.InteropServices;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.Management;

namespace AdvancedSafety
{
    public static class PortalHiding
    {
        public static void OnApplicationStart()
        {
            NativePatchUtils.NativePatch(
                typeof(ObjectInstantiator).GetMethod(nameof(ObjectInstantiator._InstantiateObject))!,
                out ourDelegate, InstantiateObjectPatch);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InstantiateObjectDelegate(IntPtr thisPtr, IntPtr objectName, Vector3 position,
            Quaternion rotation, int networkId, IntPtr player);

        private static InstantiateObjectDelegate ourDelegate;
        
        private static void InstantiateObjectPatch(IntPtr thisPtr, IntPtr objectNamePtr, Vector3 position, Quaternion rotation, int networkId, IntPtr playerPtr)
        {
            try
            {
                ourDelegate(thisPtr, objectNamePtr, position, rotation, networkId, playerPtr);

                if (!AdvancedSafetySettings.HidePortalsFromBlockedUsers.Value &&
                    !AdvancedSafetySettings.HidePortalsFromNonFriends.Value &&
                    !AdvancedSafetySettings.HidePortalsCreatedTooClose.Value || playerPtr == IntPtr.Zero) 
                    return;

                var player = new Player(playerPtr);
                var objectName = IL2CPP.Il2CppStringToManaged(objectNamePtr);

                if (objectName != "Portals/PortalInternalDynamic") return;
                var apiUser = player.prop_APIUser_0;
                if (apiUser == null) return;
                if (APIUser.CurrentUser?.id == apiUser.id) return;
                
                if (MelonDebug.IsEnabled())
                    AdvancedSafetyMod.Logger.Msg($"User {apiUser.displayName} dropped a portal");

                string denyReason = null;
                if (AdvancedSafetySettings.HidePortalsFromBlockedUsers.Value && IsBlockedEitherWay(apiUser.id))
                    denyReason = $"Disabling portal from ͏blocked user {apiUser.displayName}";
                else if(AdvancedSafetySettings.HidePortalsFromNonFriends.Value && !APIUser.IsFriendsWith(apiUser.id))
                    denyReason = $"Disabling portal from non-friend {apiUser.displayName}";
                else if(AdvancedSafetySettings.HidePortalsCreatedTooClose.Value && VRCPlayer.field_Internal_Static_VRCPlayer_0 != null && Vector3.Distance(position, VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position) < .5f)
                    denyReason = $"Disabling portal from {apiUser.displayName}/{apiUser.id} because it was dropped too close to local player";
                
                if (denyReason == null) return;
                
                var instantiator = new ObjectInstantiator(thisPtr);
                var dict = instantiator.field_Private_Dictionary_2_Int32_ObjectInfo_0;
                if (dict.ContainsKey(networkId))
                {
                    var someStruct = dict[networkId];
                    AdvancedSafetyMod.Logger.Msg(denyReason);
                    MelonCoroutines.Start(HideGameObjectAfterDelay(someStruct.field_Public_GameObject_0));
                }
            }
            catch (Exception ex)
            {
                AdvancedSafetyMod.Logger.Error($"Exception in portal hider patch: {ex}");
            }
        }

        private static IEnumerator HideGameObjectAfterDelay(GameObject go)
        {
            yield return null; // let it get initialized properly for photon
            yield return null;

            if (go != null) go.SetActive(false);
        }

        private static bool IsBlockedEitherWay(string userId)
        {
            if (userId == null) return false;
            
            var moderationManager = ModerationManager.prop_ModerationManager_0;
            if (moderationManager == null) return false;
            if (APIUser.CurrentUser?.id == userId)
                return false;
            
            var moderationsDict = ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0;
            if (!moderationsDict.ContainsKey(userId)) return false;
            
            foreach (var playerModeration in moderationsDict[userId])
            {
                if (playerModeration != null && playerModeration.moderationType == ApiPlayerModeration.ModerationType.Block)
                    return true;
            }
            
            return false;
            
        }
    }
}