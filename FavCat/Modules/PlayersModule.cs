using System;
using System.Collections.Generic;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database.Stored;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;

namespace FavCat.Modules
{
    public class PlayersModule : ExtendedFavoritesModuleBase<StoredPlayer>
    {
        internal static PageUserInfo PageUserInfo;
        
        public PlayersModule() : base(ExpandedMenu.SocialMenu, FavCatMod.Database.PlayerFavorites, GetListsParent(), false)
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu).AddSimpleButton("Local Favorite", ShowFavMenu);
            
            PageUserInfo = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo").GetComponent<PageUserInfo>();
        }
        
        private void ShowFavMenu()
        {
            var availableListsMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            var currentUser = PageUserInfo.user;
            
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
                        
                        if (FavCatSettings.IsHidePopupAfterFav) availableListsMenu.Hide();
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

        protected override void OnPickerSelected(IPickerElement picker)
        {
            APIUser.FetchUser(picker.Id, new Action<APIUser>(ShowUserPage), new Action<string>(_ => { }));
        }

        protected override void OnFavButtonClicked(StoredCategory storedCategory)
        {
            throw new NotSupportedException(); // not happening
        }

        protected override bool FavButtonsOnLists => false;
        protected override void SortModelList(string sortCriteria, string category, List<StoredPlayer> list)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<StoredPlayer> comparison;
            switch (sortCriteria)
            {
                case "name":
                case "!name":
                default:
                    comparison = (a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase) * (inverted ? -1 : 1); 
                    break;
                case "added":
                case "!added":
                    comparison = (a, b) => Favorites.GetFavoritedTime(a.PlayerId, category).CompareTo(Favorites.GetFavoritedTime(b.PlayerId, category)) * (inverted ? -1 : 1);
                    break;
            }
            list.Sort(comparison);
        }

        protected override IPickerElement WrapModel(StoredPlayer model) => new DbPlayerAdapter(model);

        protected internal override void RefreshFavButtons()
        {
            // do nothing
        }

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

            PageUserInfo
                .Method_Public_Void_APIUser_EnumNPublicSealedvaNoOnOfSeReBlInFa9vUnique_EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique_0(
                    user, friendState, UiUserList.EnumNPublicSealedvaNoInFrOnOfSeInFa9vUnique.FavoriteFriends);
        }
    }
}