using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FriendsPlusHome;
using Harmony;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC.Core;

[assembly:MelonInfo(typeof(FriendsPlusHomeMod), "Friends+ Home", "1.1.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace FriendsPlusHome
{
    public class FriendsPlusHomeMod : CustomizedMelonMod
    {
        private const string SettingsCategory = "FriendsPlusHome";
        private const string SettingStartupName = "StartupWorldType";
        private const string SettingButtonName = "GoHomeWorldType";

        private static MelonPreferences_Entry<string> StartupName;
        private static MelonPreferences_Entry<string> ButtonName;

        public override void OnApplicationStart()
        {
            var category = MelonPreferences.CreateCategory(SettingsCategory, "Friends+ Home");
            StartupName = (MelonPreferences_Entry<string>) category.CreateEntry(SettingStartupName, nameof(ApiWorldInstance.AccessType.FriendsOfGuests), "Startup instance type");
            ButtonName = (MelonPreferences_Entry<string>) category.CreateEntry(SettingButtonName, nameof(ApiWorldInstance.AccessType.FriendsOfGuests), "\"Go Home\" instance type");

            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit" && !it.Info.Version.StartsWith("0.1."))) 
                RegisterUix2Extension();

            foreach (var methodInfo in typeof(VRCFlowManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (methodInfo.ReturnType != typeof(void) || methodInfo.GetParameters().Length != 0) 
                    continue;
                
                if (!XrefScanner.XrefScan(methodInfo).Any(it => it.Type == XrefType.Global && it.ReadAsObject()?.ToString() == "Going to Home Location: ")) 
                    continue;
                
                MelonLogger.Msg($"Patched {methodInfo.Name}");
                Harmony.Patch(methodInfo, postfix: new HarmonyMethod(AccessTools.Method(typeof(FriendsPlusHomeMod), nameof(GoHomePatch))));
            }
            
            DoAfterUiManagerInit(OnUiManagerInit);
        }

        private void OnUiManagerInit()
        {
            StartEnforcingInstanceType(VRCFlowManager.prop_VRCFlowManager_0, false); // just in case startup is slow
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterUix2Extension()
        {
            var possibleValues = new []
            {
                (nameof(ApiWorldInstance.AccessType.Public), "Public"),
                (nameof(ApiWorldInstance.AccessType.FriendsOfGuests), "Friends+"),
                (nameof(ApiWorldInstance.AccessType.FriendsOnly), "Friends only"),
                (nameof(ApiWorldInstance.AccessType.InvitePlus), "Invite+"),
                (nameof(ApiWorldInstance.AccessType.InviteOnly), "Invite only"),
            };
            ExpansionKitApi.RegisterSettingAsStringEnum(SettingsCategory, SettingStartupName, possibleValues);
            ExpansionKitApi.RegisterSettingAsStringEnum(SettingsCategory, SettingButtonName, possibleValues);
        }

        private static void GoHomePatch(VRCFlowManagerVRC __instance)
        {
            StartEnforcingInstanceType(__instance, true);
        }

        private static void StartEnforcingInstanceType(VRCFlowManager flowManager, bool isButton)
        {
            var targetType = Enum.TryParse<ApiWorldInstance.AccessType>(isButton ? ButtonName.Value : StartupName.Value, out var type) ? type : ApiWorldInstance.AccessType.FriendsOfGuests;
            MelonLogger.Msg($"Enforcing home instance type: {targetType}");
            flowManager.field_Protected_AccessType_0 = targetType;

            MelonCoroutines.Start(EnforceTargetInstanceType(flowManager, targetType, isButton ? 10 : 30));
        }
        
        private static int ourRequestId;
        
        private static IEnumerator EnforceTargetInstanceType(VRCFlowManager manager, ApiWorldInstance.AccessType type, float time)
        {
            var endTime = Time.time + time;
            var currentRequestId = ++ourRequestId;
            while (Time.time < endTime && ourRequestId == currentRequestId)
            {
                manager.field_Protected_AccessType_0 = type;
                yield return null;
            }
        }
    }
}