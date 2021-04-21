using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;

namespace FavCat.Modules
{
    public sealed class AvatarModule : ExtendedFavoritesModuleBase<StoredAvatar>
    {
        private readonly string myCurrentAnnoyingMessage;
        
        private readonly PageAvatar myPageAvatar;
        
        private string myCurrentUiAvatarId = "";

        private readonly bool myInitialised;

        public AvatarModule() : base(ExpandedMenu.AvatarMenu, FavCatMod.Database.AvatarFavorites, GetListsParent(), false, DateTime.Now < FavCatMod.NoMoreVisibleAvatarFavoritesAfter)
        {
            myCurrentAnnoyingMessage = CanPerformAdditiveActions ? "WillBeObsolete" : (CanShowExistingLists ? "CantAdd" : "NoFavorites");
            
            MelonLogger.Log("Adding button to UI - Looking up for Change Button");
            var foundAvatarPage = Resources.FindObjectsOfTypeAll<PageAvatar>()?.FirstOrDefault(p => p.transform.Find("Change Button") != null);
            if (foundAvatarPage == null)
                throw new ApplicationException("No avatar page, can't initialize extended favorites");
            
            myPageAvatar = foundAvatarPage;
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu).AddSimpleButton("Search known public avatars", DoSearchKnownAvatars);

            var expandEnforcer = new GameObject(ExpandEnforcerGameObjectName, new[] {Il2CppType.Of<RectTransform>(), Il2CppType.Of<LayoutElement>()});
            expandEnforcer.transform.SetParent(GetListsParent(), false);
            var layoutElement = expandEnforcer.GetComponent<LayoutElement>();
            layoutElement.minWidth = 1534;
            layoutElement.minHeight = 0;
            
            myPageAvatar.gameObject.AddComponent<EnableDisableListener>().OnEnabled += () =>
            {
                if (FavCatSettings.DontShowAnnoyingMessage == myCurrentAnnoyingMessage || myHasShownAnnoyingMessageThisRun) return;
                myHasShownAnnoyingMessageThisRun = true;
                ShowAnnoyingMessage();
            };

            myInitialised = true;
        }


        private bool myHasShownAnnoyingMessageThisRun = false;

        private void DoSearchKnownAvatars()
        {
            if (PlayersModule.PageUserInfo == null)
                return;
            
            VRCUiManager.prop_VRCUiManager_0.Method_Public_Void_String_Boolean_0("UserInterface/MenuContent/Screens/Avatar", false);
            SetSearchListHeaderAndScrollToIt("Search running...");
            LastSearchRequest = "Created by " + PlayersModule.PageUserInfo.field_Public_APIUser_0.displayName;
            FavCatMod.Database.RunBackgroundAvatarSearchByUser(PlayersModule.PageUserInfo.field_Public_APIUser_0.id, AcceptSearchResult);
        }

        private static Transform GetListsParent()
        {
            var foundAvatarPage = GameObject.Find("UserInterface/MenuContent/Screens/Avatar").GetComponent<PageAvatar>();
            if (foundAvatarPage == null)
                throw new ApplicationException("No avatar page, can't initialize extended favorites");

            var randomList = foundAvatarPage.GetComponentInChildren<UiAvatarList>();
            return randomList.transform.parent;
        }
        
        protected override void OnFavButtonClicked(StoredCategory storedCategory)
        {
            // do nothing
        }

        protected internal override void RefreshFavButtons()
        {
            var apiAvatar = myPageAvatar != null ? myPageAvatar.field_Public_SimpleAvatarPedestal_0 != null ? myPageAvatar.field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 : null : null;

            foreach (var customPickerList in PickerLists)
            {
                bool favorited = FavCatMod.Database.AvatarFavorites.IsFavorite(myCurrentUiAvatarId, customPickerList.Key);
                    
                var isNonPublic = apiAvatar?.releaseStatus != "public";
                var enabled = !isNonPublic || favorited || apiAvatar?.authorId == APIUser.CurrentUser.id;
                enabled &= CanPerformAdditiveActions;
                if (favorited)
                    customPickerList.Value.SetFavButtonText(isNonPublic ? "Unfav (p)" : "Unfav", false);
                else
                    customPickerList.Value.SetFavButtonText(isNonPublic ? (enabled ? "Fav (p)" : "Private") : "Fav", false);
            }
        }

        protected override void OnPickerSelected(IPickerElement model)
        {
            PlaySound();

            if (!CanShowExistingLists)
            {
                FavCatMod.Instance.PlayerModule?.OnPickerSelected(((IStoredModelAdapter<StoredAvatar>) model).Model.AuthorId, listsParent.gameObject);
                return;
            }

            var avatar = new ApiAvatar() {id = model.Id};
            if (Imports.IsDebugMode())
                MelonLogger.Log($"Performing an API request for {model.Id}");
            avatar.Fetch(new Action<ApiContainer>((_) =>
            {
                if (Imports.IsDebugMode())
                    MelonLogger.Log($"Done an API request for {model.Id}");

                FavCatMod.Database?.UpdateStoredAvatar(avatar);

                var canUse = avatar.releaseStatus == "public" || avatar.authorId == APIUser.CurrentUser.id;
                if (!canUse)
                {
                    myPageAvatar.field_Public_SimpleAvatarPedestal_0.DisplayErrorAvatar();
                    myPageAvatar.field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = avatar; // set it directly here because refreshing will load it
                }
                else
                    myPageAvatar.field_Public_SimpleAvatarPedestal_0.Refresh(avatar);

                // VRC has a tendency to change visibility of its lists after pedestal refresh 
                ReorderLists();
                RefreshFavButtons();
            }), new Action<ApiContainer>(c =>
            {
                if (Imports.IsDebugMode())
                    MelonLogger.Log("API request errored with " + c.Code + " - " + c.Error);
                if (c.Code == 404 && listsParent.gameObject.activeInHierarchy)
                {
                    FavCatMod.Database.CompletelyDeleteAvatar(model.Id);
                    var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddLabel("This avatar is not available anymore (deleted or privated)");
                    menu.AddLabel("It has been removed from all favorite lists");
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSimpleButton("Close", menu.Hide);
                    menu.Show();
                }
            }));
        }

        internal override void Update()
        {
            if (!myInitialised) return;
            
            base.Update();

            var pedestal = myPageAvatar.field_Public_SimpleAvatarPedestal_0;
            if (pedestal == null) return;
            var apiAvatar = pedestal.field_Internal_ApiAvatar_0;
            if (apiAvatar == null) return;
            if (apiAvatar.id == myCurrentUiAvatarId) return;
            
            myCurrentUiAvatarId = apiAvatar.id ?? "";

            RefreshFavButtons();
            if (apiAvatar.Populated) 
                FavCatMod.Database?.UpdateStoredAvatar(apiAvatar);
        }

        protected override void SearchButtonClicked()
        {
            BuiltinUiUtils.ShowInputPopup("Local Search (Avatar)", "", InputField.InputType.Standard, false,
                "Search!", (s, list, arg3) =>
                {
                    SetSearchListHeaderAndScrollToIt("Search running...");
                    LastSearchRequest = s;
                    FavCatMod.Database.RunBackgroundAvatarSearch(s, AcceptSearchResult);
                });
        }

        protected override bool FavButtonsOnLists => true;
        protected override IPickerElement WrapModel(StoredFavorite? favorite, StoredAvatar model) => new DbAvatarAdapter(model, favorite);

        protected override void SortModelList(string sortCriteria, string category, List<(StoredFavorite?, StoredAvatar)> avatars)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<(StoredFavorite? Fav, StoredAvatar Model)> comparison;
            switch (sortCriteria)
            {
                case "name":
                case "!name":
                default:
                    comparison = (a, b) => string.Compare(a.Model.Name, b.Model.Name, StringComparison.InvariantCultureIgnoreCase) * (inverted ? -1 : 1); 
                    break;
                case "updated":
                case "!updated":
                    comparison = (a, b) => a.Model.UpdatedAt.CompareTo(b.Model.UpdatedAt) * (inverted ? -1 : 1);
                    break;
                case "created":
                case "!created":
                    comparison = (a, b) => a.Model.CreatedAt.CompareTo(b.Model.CreatedAt) * (inverted ? -1 : 1);
                    break;
                case "added":
                case "!added":
                    comparison = (a, b) => (a.Fav?.AddedOn ?? DateTime.MinValue).CompareTo(b.Fav?.AddedOn ?? DateTime.MinValue) * (inverted ? -1 : 1);
                    break;
            }
            avatars.Sort(comparison);
        }

        public override void ShowAnnoyingMessage()
        {
            var popup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            popup.AddLabel("Due to recent events, avatar favorites in FavCat are being phased out.");
            popup.AddLabel($"To read more about this, click this button:");
            popup.AddSimpleButton($"More info (opens in browser)", () => Process.Start("https://github.com/knah/VRCMods#avatar-favorites-deprecation"));
            popup.AddLabel("You can't add new avatar favorites or create new lists");

            popup.AddLabel(CanShowExistingLists
                ? $"You will no longer be able to see existing avatar favorites starting on {FavCatMod.NoMoreVisibleAvatarFavoritesAfter.ToShortDateString()}"
                : "You can't see existing avatar favorite lists. You still can export them.");

            popup.AddLabel("World and user favorites will remain for the time being");
            popup.AddLabel("Favorite export is available from \"More FavCat...\" menu");
            popup.AddLabel("Scroll down for close buttons");
            popup.AddSimpleButton("Don't show this until game restart", popup.Hide);
            popup.AddSimpleButton("Don't show this until something changes", () =>
            {
                popup.Hide();
                FavCatSettings.DontShowAnnoyingMessage = myCurrentAnnoyingMessage;
            });
            
            popup.Show();
        }
    }
}