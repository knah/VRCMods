using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Harmony;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using VRC;
using VRC.Core;

namespace AdvancedSafety
{
    public static class AvatarHiding
    {
        private const string BlockedAvatarsMakersFilePath = "UserData\\blocked_avatar_authors.txt";
        private const string BlockedAvatarsFilePath = "UserData\\blocked_avatars.txt";
        
        internal static readonly Dictionary<string, string> ourBlockedAvatarAuthors = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> ourBlockedAvatars = new Dictionary<string, string>();
        
        public static void OnApplicationStart(HarmonyInstance harmony)
        {
            if (File.Exists(BlockedAvatarsMakersFilePath))
            {
                ourBlockedAvatarAuthors.Clear();
                foreach (var it in File.ReadAllLines(BlockedAvatarsMakersFilePath, Encoding.UTF8))
                {
                    var split = it.Split(new[] {'͏', ' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 2)
                        ourBlockedAvatarAuthors[split[0].Trim()] = split[1];
                }
            }
            
            if (File.Exists(BlockedAvatarsFilePath))
            {
                ourBlockedAvatars.Clear();
                foreach (var it in File.ReadAllLines(BlockedAvatarsFilePath, Encoding.UTF8))
                {
                    var split = it.Split(new[] {'͏', ' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 2)
                        ourBlockedAvatars[split[0].Trim()] = split[1];
                }
            }

            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(VRCAvatarManager).GetMethod(
                        nameof(VRCAvatarManager.Method_Public_UniTask_ApiAvatar_Single_Action_1_Boolean_0)))
                    .GetValue(null);
                
                MelonUtils.NativeHookAttach((IntPtr)(&originalMethodPointer), typeof(AvatarHiding).GetMethod(nameof(SwitchAvatarPatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourSwitchAvatar = Marshal.GetDelegateForFunctionPointer<SwitchAvatarDelegate>(originalMethodPointer);
            }

            foreach (var methodInfo in typeof(FeaturePermissionManager).GetMethods()
                .Where(it =>
                    it.Name.StartsWith("Method_Public_Boolean_APIUser_byref_EnumPublicSealedva5vUnique_") &&
                    it.GetCustomAttribute<CallerCountAttribute>().Count > 0))
            {
                harmony.Patch(methodInfo,
                    postfix: new HarmonyMethod(typeof(AvatarHiding), nameof(CanUseCustomAvatarPostfix)));
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr SwitchAvatarDelegate(IntPtr hiddenValueTypeReturn, IntPtr thisPtr, IntPtr apiAvatarPtr, float someFloat, IntPtr someDelegate, IntPtr nativeMethodInfo);

        private static SwitchAvatarDelegate ourSwitchAvatar;
        
        private static IntPtr SwitchAvatarPatch(IntPtr hiddenStructReturn, IntPtr thisPtr, IntPtr apiAvatarPtr, float someFloat, IntPtr someDelegate, IntPtr nativeMethodInfo)
        {
            using (new SwitchAvatarCookie(new VRCAvatarManager(thisPtr), apiAvatarPtr == IntPtr.Zero ? null : new ApiAvatar(apiAvatarPtr)))
                return ourSwitchAvatar(hiddenStructReturn, thisPtr, apiAvatarPtr, someFloat, someDelegate, nativeMethodInfo);
        }

        private static void CanUseCustomAvatarPostfix(ref bool __result)
        {
            try
            {
                if (!SwitchAvatarCookie.ourInSwitch || SwitchAvatarCookie.ourApiAvatar == null)
                    return;
                
                var apiAvatar = SwitchAvatarCookie.ourApiAvatar;
                var avatarManager = SwitchAvatarCookie.ourAvatarManager;

                var vrcPlayer = avatarManager.field_Private_VRCPlayer_0;
                if (vrcPlayer == null) return;

                if (vrcPlayer == VRCPlayer.field_Internal_Static_VRCPlayer_0) // never apply to self
                    return;

                var apiUser = vrcPlayer.prop_Player_0?.prop_APIUser_0;
                if (apiUser == null)
                    return;

                var userId = apiUser.id;
                if (!AdvancedSafetySettings.IncludeFriendsInHides.Value && APIUser.IsFriendsWith(userId))
                    return;

                if (AdvancedSafetySettings.HidesAbideByShowAvatar.Value &&
                    AdvancedSafetyMod.IsAvatarExplicitlyShown(userId))
                    return;

                if (ourBlockedAvatarAuthors.ContainsKey(apiAvatar.authorId) ||
                    ourBlockedAvatars.ContainsKey(apiAvatar.id))
                {
                    MelonLogger.Msg(
                        $"Hiding avatar on {apiUser.displayName} because it or its author is hidden");
                    // denyReason = 3;
                    __result = false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception in CanUseCustomAvatarPatch: {ex}");
            }
        }

        private struct SwitchAvatarCookie : IDisposable
        {
            internal static bool ourInSwitch;
            internal static VRCAvatarManager ourAvatarManager;
            internal static ApiAvatar ourApiAvatar;
            
            public SwitchAvatarCookie(VRCAvatarManager avatarManager, ApiAvatar apiAvatar)
            {
                ourAvatarManager = avatarManager;
                ourApiAvatar = apiAvatar;
                ourInSwitch = true;
            }
            
            public void Dispose()
            {
                ourApiAvatar = null;
                ourAvatarManager = null;
                ourInSwitch = false;
            }
        }


        public static void SaveBlockedAuthors()
        {
            File.WriteAllLines(BlockedAvatarsMakersFilePath, ourBlockedAvatarAuthors.Select(it => $"{it.Key} ͏{it.Value}"), Encoding.UTF8);
        }
        
        public static void SaveBlockedAvatars()
        {
            File.WriteAllLines(BlockedAvatarsFilePath, ourBlockedAvatars.Select(it => $"{it.Key} ͏{it.Value}"), Encoding.UTF8);
        }
    }
}