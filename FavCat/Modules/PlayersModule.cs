using System;
using System.Collections.Generic;
using System.Linq;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;

namespace FavCat.Modules
{
    public class PlayersModule : ExtendedFavoritesModuleBase<StoredPlayer>
    {
        internal static PageUserInfo PageUserInfo;
        
        public PlayersModule() : base(ExpandedMenu.SocialMenu, FavCatMod.Database.PlayerFavorites, GetListsParent(), true, true, false)
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu).AddSimpleButton("Local Favorite", ShowFavMenu);
            
            PageUserInfo = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo").GetComponent<PageUserInfo>();
        }
        
        private void ShowFavMenu()
        {
            var availableListsMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            var currentUser = PageUserInfo.field_Public_APIUser_0;
            
            var storedCategories = GetCategoriesInSortedOrder();

            if (storedCategories.Count == 0)
                availableListsMenu.AddLabel("Create some categories first before favoriting players!");
            
            availableListsMenu.AddSimpleButton("Close", () => availableListsMenu.Hide());

            foreach (var storedCategory in storedCategories)
            {
                if (storedCategory.CategoryName == SearchCategoryName)
                    continue;

                Text? buttonText = null;
                availableListsMenu.AddSimpleButton(
                    $"{(!Favorites.IsFavorite(currentUser.id, storedCategory.CategoryName) ? "Favorite to" : "Unfavorite from")} {storedCategory.CategoryName}", 
                    () =>
                    {
                        if (Favorites.IsFavorite(currentUser.id, storedCategory.CategoryName))
                            Favorites.DeleteFavorite(currentUser.id, storedCategory.CategoryName);
                        else
                            Favorites.AddFavorite(currentUser.id, storedCategory.CategoryName);

                        buttonText!.text = $"{(!Favorites.IsFavorite(currentUser.id, storedCategory.CategoryName) ? "Favorite to" : "Unfavorite from")} {storedCategory.CategoryName}";
                        
                        if (FavCatSettings.HidePopupAfterFav.Value) availableListsMenu.Hide();
                    }, 
                    o => buttonText = o.GetComponentInChildren<Text>());
            }
            
            availableListsMenu.Show();
        }

        private static Transform GetListsParent()
        {
            var foundSocialPage = GameObject.Find("UserInterface/MenuContent/Screens/Social");
            if (foundSocialPage == null)
                throw new ApplicationException("No social page, can't initialize extended favorites");

            var randomList = foundSocialPage.GetComponentInChildren<UiUserList>(true);
            return randomList.transform.parent;
        }

        private string myLastRequestedPlayer = "";
        protected override void OnPickerSelected(IPickerElement picker) => OnPickerSelected(picker.Id, listsParent.gameObject);
        
        public void OnPickerSelected(string playerId, GameObject whichObjectToCheck)
        {
            if (playerId == myLastRequestedPlayer) 
                return;
            
            PlaySound();

            myLastRequestedPlayer = playerId;
            var user = new APIUser {id = playerId};
            user.Fetch(new Action<ApiContainer>(_ =>
            {
                myLastRequestedPlayer = "";
                if (whichObjectToCheck.activeInHierarchy)
                    ShowUserPage(user);
            }), new Action<ApiContainer>(c =>
            {
                myLastRequestedPlayer = "";
                if (MelonDebug.IsEnabled())
                    MelonDebug.Msg("API request errored with " + c.Code + " - " + c.Error);
                if (c.Code == 404 && whichObjectToCheck.activeInHierarchy)
                {
                    FavCatMod.Database.CompletelyDeletePlayer(playerId);
                    var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddLabel("This player is not available anymore (deleted)");
                    menu.AddLabel("It has been removed from all favorite lists");
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSimpleButton("Close", menu.Hide);
                    menu.Show();
                }
            }));
        }

        protected override void SortModelList(string sortCriteria, string category, List<(StoredFavorite?, StoredPlayer)> avatars)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<(StoredFavorite? Fav, StoredPlayer Model)> comparison;
            switch (sortCriteria)
            {
                case "name":
                case "!name":
                default:
                    comparison = (a, b) => string.Compare(a.Model.Name, b.Model.Name, StringComparison.InvariantCultureIgnoreCase) * (inverted ? -1 : 1); 
                    break;
                case "added":
                case "!added":
                    comparison = (a, b) => (a.Fav?.AddedOn ?? DateTime.MinValue).CompareTo(b.Fav?.AddedOn ?? DateTime.MinValue) * (inverted ? -1 : 1);
                    break;
            }
            avatars.Sort(comparison);
        }

        protected override IPickerElement WrapModel(StoredFavorite? favorite, StoredPlayer model) => new DbPlayerAdapter(model, favorite);

        protected override void SearchButtonClicked()
        {
            BuiltinUiUtils.ShowInputPopup("Local Search (Player)", "", InputField.InputType.Standard, false,
                "Search!", (s, list, arg3) =>
                {
                    SetSearchListHeaderAndScrollToIt("Search running...");
                    LastSearchRequest = s;
                    FavCatMod.Database.RunBackgroundPlayerSearch(s, AcceptSearchResult);
                });
        }

        private void ShowUserPage(APIUser user)
        {
            VRCUiManager.prop_VRCUiManager_0.Method_Public_Void_String_Boolean_0("UserInterface/MenuContent/Screens/UserInfo", true);
            var friendState = APIUser.IsFriendsWith(user.id)
                ? (user.statusValue == APIUser.UserStatus.Offline
                    ? PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique.OfflineFriend
                    : PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique.OnlineFriend)
                : PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique.NotFriends;

            SetUserPageUser(PageUserInfo, user, friendState, UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique.FavoriteFriends);
        }

        private static
            Action<PageUserInfo, APIUser, PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique,
                UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique>? ourSetUserInfo;

        private void SetUserPageUser(PageUserInfo pageUserInfo, APIUser user, PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique enumA,
            UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique enumB)
        {
            if (ourSetUserInfo == null)
            {
                var targetMethod = typeof(PageUserInfo).GetMethods().Single(it =>
                    it.Name.StartsWith("Method_Public_Void_APIUser_EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique_EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique_") && XrefScanner.XrefScan(it).Any(jt => jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "online in current world"));
                ourSetUserInfo = (Action<PageUserInfo, APIUser, PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique,
                    UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique>) Delegate.CreateDelegate(typeof(Action<PageUserInfo, APIUser, PageUserInfo.EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique,
                    UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique>), targetMethod);
            }

            ourSetUserInfo(pageUserInfo, user, enumA, enumB);
        }
    }
}