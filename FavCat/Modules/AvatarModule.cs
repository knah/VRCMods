using System;
using System.Collections.Generic;
using System.Linq;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;

namespace FavCat.Modules
{
    public class AvatarModule : ExtendedFavoritesModuleBase<StoredAvatar>
    {
        private readonly PageAvatar myPageAvatar;
        
        private string myCurrentUiAvatarId = "";

        private readonly bool myInitialised;

        public AvatarModule() : base(ExpandedMenu.AvatarMenu, FavCatMod.Database.AvatarFavorites, GetListsParent())
        {
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

            myInitialised = true;
        }

        private void DoSearchKnownAvatars()
        {
            if (PlayersModule.PageUserInfo == null)
                return;
            
            VRCUiManager.prop_VRCUiManager_0.Method_Public_Void_String_Boolean_0("UserInterface/MenuContent/Screens/Avatar", false);
            SetSearchListHeaderAndScrollToIt("Search running...");
            LastSearchRequest = "Created by " + PlayersModule.PageUserInfo.user.displayName;
            FavCatMod.Database.RunBackgroundAvatarSearchByUser(PlayersModule.PageUserInfo.user.id, AcceptSearchResult);
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
            ApiAvatar currentApiAvatar = myPageAvatar.avatar.field_Internal_ApiAvatar_0;
            if (FavCatMod.Database.AvatarFavorites.IsFavorite(currentApiAvatar.id, storedCategory.CategoryName))
                FavCatMod.Database.AvatarFavorites.DeleteFavorite(currentApiAvatar.id, storedCategory.CategoryName);
            else
                FavCatMod.Database.AvatarFavorites.AddFavorite(currentApiAvatar.id, storedCategory.CategoryName);
        }

        protected internal override void RefreshFavButtons()
        {
            var apiAvatar = myPageAvatar != null ? myPageAvatar.avatar != null ? myPageAvatar.avatar.field_Internal_ApiAvatar_0 : null : null;

            foreach (var customPickerList in PickerLists)
            {
                bool favorited = FavCatMod.Database.AvatarFavorites.IsFavorite(myCurrentUiAvatarId, customPickerList.Key);
                    
                var isNonPublic = apiAvatar?.releaseStatus != "public";
                var enabled = !isNonPublic || favorited || apiAvatar?.authorId == APIUser.CurrentUser.id;
                if (favorited)
                    customPickerList.Value.SetFavButtonText(isNonPublic ? "Unfav (p)" : "Unfav", true);
                else
                    customPickerList.Value.SetFavButtonText(isNonPublic ? (enabled ? "Fav (p)" : "Private") : "Fav", enabled);
            }
        }

        protected override void OnPickerSelected(IPickerElement model)
        {
            var avatar = new ApiAvatar() {id = model.Id};
            MelonLogger.Log($"Performing an API request for {model.Id}");
            avatar.Fetch(new Action<ApiContainer>((_) =>
            {
                MelonLogger.Log($"Done an API request for {model.Id}");

                var canUse = avatar.releaseStatus == "public" || avatar.authorId == APIUser.CurrentUser.id;
                if (!canUse)
                {
                    myPageAvatar.avatar.DisplayErrorAvatar();
                    myPageAvatar.avatar.field_Internal_ApiAvatar_0 = avatar; // set it directly here because refreshing will load it
                }
                else
                    myPageAvatar.avatar.Refresh(avatar);

                RefreshFavButtons();
            }));
        }

        internal override void Update()
        {
            if (!myInitialised) return;

            if (myPageAvatar.avatar != null && myPageAvatar.avatar.field_Internal_ApiAvatar_0 != null &&
                !myCurrentUiAvatarId.Equals(myPageAvatar.avatar.field_Internal_ApiAvatar_0?.id))
            {
                var apiAvatar = myPageAvatar != null ? myPageAvatar.avatar != null ? myPageAvatar.avatar.field_Internal_ApiAvatar_0 : null : null;
                
                myCurrentUiAvatarId = apiAvatar?.id ?? "";

                RefreshFavButtons();
            }
            
            base.Update();
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
        protected override IPickerElement WrapModel(StoredAvatar model) => new DbAvatarAdapter(model);

        protected override void SortModelList(string sortCriteria, string category, List<StoredAvatar> avatars)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<StoredAvatar> comparison;
            switch (sortCriteria)
            {
                case "name":
                case "!name":
                default:
                    comparison = (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase) * (inverted ? -1 : 1); 
                    break;
                case "updated":
                case "!updated":
                    comparison = (a, b) => a.UpdatedAt.CompareTo(b.UpdatedAt) * (inverted ? -1 : 1);
                    break;
                case "created":
                case "!created":
                    comparison = (a, b) => a.CreatedAt.CompareTo(b.CreatedAt) * (inverted ? -1 : 1);
                    break;
                case "added":
                case "!added":
                    comparison = (a, b) => FavCatMod.Database.AvatarFavorites.GetFavoritedTime(a.AvatarId, category).CompareTo(FavCatMod.Database.AvatarFavorites.GetFavoritedTime(b.AvatarId, category)) * (inverted ? -1 : 1);
                    break;
            }
            avatars.Sort(comparison);
        }
    }
}