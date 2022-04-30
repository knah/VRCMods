using System;
using System.Collections;
using AdvancedSafety.BundleVerifier;
using MelonLoader;
using UIExpansionKit;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.DataModel;
using VRC.UI;
using VRC.UI.Elements.Menus;

namespace AdvancedSafety
{
    public static class UiExpansionKitSupport
    {
        private static PageUserInfo ourUserInfoPage;

        private static SelectedUserMenuQM ourSelectedUserQm;

        private static Action<string> ourHideAuthorTextSink;
        private static Action<string> ourHideAvatarTextSink;

        private static Action<bool> ourForceAllowBundleSink;

        public static void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<QuickMenuHideAvatarButtonHandler>();
            
            var hideAuthorButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu).AddSimpleButton("Hide all avatars by this author", OnHideBigClick);
            var hideAvatarButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("Hide this avatar (on anyone)", OnHideAvatarClick);
            var forceAllowBundleButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("Force allow this avatar bundle", OnForceAllowBundleClick);

            ourHideAuthorTextSink = s => hideAuthorButton.Text = s;
            ourHideAvatarTextSink = s => hideAvatarButton.Text = s;
            ourForceAllowBundleSink = b => forceAllowBundleButton.Visible = b;
            
            hideAvatarButton.OnInstanceCreated += obj => obj.AddComponent<QuickMenuHideAvatarButtonHandler>();

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Reload all avatars", () => ScanningReflectionCache.ReloadAllAvatars(false));

            ExpansionKitApi.GetSettingsCategory(BundleVerifierMod.SettingsCategory)
                .AddSimpleButton("Reset corrupted bundle cache", () => BundleVerifierMod.BadBundleCache.Clear());

            MelonCoroutines.Start(InitThings());
        }

        internal static IUser GetUserSelectedInQm()
        {
            if (ourSelectedUserQm == null)
                ourSelectedUserQm = UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_SelectedUser_Local")
                    ?.GetComponent<SelectedUserMenuQM>();

            return ourSelectedUserQm?.field_Private_IUser_0;
        }

        private static void OnHideAvatarClick()
        {
            var apiAvatar = GetUserSelectedInQm()?.GetPlayer()?._vrcplayer?.prop_VRCAvatarManager_0?.field_Private_ApiAvatar_0;
            if (apiAvatar == null) return;

            if (AvatarHiding.ourBlockedAvatars.ContainsKey(apiAvatar.id))
                AvatarHiding.ourBlockedAvatars.Remove(apiAvatar.id);
            else
                AvatarHiding.ourBlockedAvatars[apiAvatar.id] = apiAvatar.name;
            
            AvatarHiding.SaveBlockedAvatars();
            
            ScanningReflectionCache.ReloadAllAvatars(true);
        }
        
        private static void OnForceAllowBundleClick()
        {
            var apiAvatar = GetUserSelectedInQm()?.GetPlayer()?._vrcplayer?.prop_VRCAvatarManager_0?.field_Private_ApiAvatar_0;
            if (apiAvatar == null) return;

            if (BundleVerifierMod.BadBundleCache.Contains(apiAvatar.assetUrl))
                BundleVerifierMod.ForceAllowedCache.Add(apiAvatar.assetUrl);

            ScanningReflectionCache.ReloadAllAvatars(true);
        }

        private static void OnHideBigClick()
        {
            var apiUser = ourUserInfoPage?.field_Private_APIUser_0;
            var userId = apiUser?.id;
            if (userId == null) return;
            
            if (AvatarHiding.ourBlockedAvatarAuthors.ContainsKey(userId))
                AvatarHiding.ourBlockedAvatarAuthors.Remove(userId);
            else
                AvatarHiding.ourBlockedAvatarAuthors[userId] = apiUser.displayName ?? "";

            AvatarHiding.SaveBlockedAuthors();

            OnPageShown(ourUserInfoPage);
            
            ScanningReflectionCache.ReloadAllAvatars(true);
        }

        private static IEnumerator InitThings()
        {
            while (AdvancedSafetyMod.GetUiManager() == null)
                yield return null;

            AdvancedSafetyMod.GetUiManager().Method_Internal_add_Void_Action_1_VRCUiPage_0(new Action<VRCUiPage>(OnPageShown));
        }

        private static void OnPageShown(VRCUiPage obj)
        {
            var userPage = obj.TryCast<PageUserInfo>();
            if (userPage == null) return;
            ourUserInfoPage = userPage;

            MelonCoroutines.Start(UpdateUserFromPage(userPage));
        }

        private static IEnumerator UpdateUserFromPage(PageUserInfo pageUserInfo)
        {
            yield return new WaitForSeconds(.5f);

            ourHideAuthorTextSink(
                AvatarHiding.ourBlockedAvatarAuthors.ContainsKey(pageUserInfo.field_Private_APIUser_0?.id ?? "")
                    ? "Unhide all avatars by this author"
                    : "Hide all avatars by this author");
        }

        public static void QuickMenuUpdateTick(VRCPlayer player)
        {
            var currentAvatar = player.prop_VRCAvatarManager_0?.field_Private_ApiAvatar_0;
            if (currentAvatar == null) return;

            ourHideAvatarTextSink(AvatarHiding.ourBlockedAvatars.ContainsKey(currentAvatar.id)
                ? "Unhide this avatar (on anyone)"
                : "Hide this avatar (on anyone)");

            ourForceAllowBundleSink(BundleVerifierMod.BadBundleCache.Contains(currentAvatar.assetUrl));
        }
    }
}