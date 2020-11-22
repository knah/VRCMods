using System;
using System.Collections;
using System.Collections.Generic;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.UI;

namespace FavCat.Modules
{
    public class WorldsModule : ExtendedFavoritesModuleBase<StoredWorld>
    {
        private readonly PageWorldInfo myPageWorldInfo;
        
        public WorldsModule() : base(ExpandedMenu.WorldMenu, FavCatMod.Database.WorldFavorites, GetListsParent())
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.WorldDetailsMenu).AddSimpleButton("Local Favorite", ShowFavMenu);

            myPageWorldInfo = GameObject.Find("UserInterface/MenuContent/Screens/WorldInfo").GetComponentInChildren<PageWorldInfo>();
            myPageWorldInfo.gameObject.AddComponent<EnableDisableListener>().OnEnabled += () =>
            {
                MelonCoroutines.Start(EnforceNewInstanceButtonEnabled());
            };
        }

        private IEnumerator EnforceNewInstanceButtonEnabled()
        {
            var endTime = Time.time + 5f;
            while (Time.time < endTime && myPageWorldInfo.gameObject.activeSelf)
            {
                yield return null;
                myPageWorldInfo.newInstanceButton.interactable = true;
            }
        } 

        private void ShowFavMenu()
        {
            var availableListsMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            var currentWorld = myPageWorldInfo.prop_ApiWorld_0;

            var storedCategories = GetCategoriesInSortedOrder();

            if (storedCategories.Count == 0)
                availableListsMenu.AddLabel("Create some categories first before favoriting worlds!");
            
            availableListsMenu.AddSimpleButton("Close", () => availableListsMenu.Hide());

            foreach (var storedCategory in storedCategories)
            {
                if (storedCategory.CategoryName == SearchCategoryName)
                    continue;

                Text? buttonText = null;
                availableListsMenu.AddSimpleButton(
                    $"{(!Favorites.IsFavorite(currentWorld.id, storedCategory.CategoryName) ? "Favorite to" : "Unfavorite from")} {storedCategory.CategoryName}", 
                    () =>
                    {
                        if (Favorites.IsFavorite(currentWorld.id, storedCategory.CategoryName))
                            Favorites.DeleteFavorite(currentWorld.id, storedCategory.CategoryName);
                        else
                            Favorites.AddFavorite(currentWorld.id, storedCategory.CategoryName);

                        buttonText!.text = $"{(!Favorites.IsFavorite(currentWorld.id, storedCategory.CategoryName) ? "Favorite to" : "Unfavorite from")} {storedCategory.CategoryName}";
                        
                        if (FavCatSettings.IsHidePopupAfterFav) availableListsMenu.Hide();
                    }, 
                    o => buttonText = o.GetComponentInChildren<Text>());
            }
            
            availableListsMenu.Show();
        }

        private static Transform GetListsParent()
        {
            var foundWorldsPage = GameObject.Find("UserInterface/MenuContent/Screens/Worlds");
            if (foundWorldsPage == null)
                throw new ApplicationException("No world page, can't initialize extended favorites");

            var randomList = foundWorldsPage.GetComponentInChildren<UiWorldList>(true);
            return randomList.transform.parent;
        }

        protected override void OnPickerSelected(IPickerElement picker)
        {
            var world = new ApiWorld {id = picker.Id};
            world.Fetch(new Action<ApiContainer>(_ =>
            {
                UiWorldList.Method_Public_Static_Void_ApiWorld_0(world);
            }));
        }

        protected override void OnFavButtonClicked(StoredCategory storedCategory)
        {
            throw new NotSupportedException(); // they aren't clicked for worlds
        }

        protected override bool FavButtonsOnLists => false;
        protected override void SortModelList(string sortCriteria, string category, List<StoredWorld> list)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<StoredWorld> comparison;
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
                    comparison = (a, b) => Favorites.GetFavoritedTime(a.WorldId, category).CompareTo(Favorites.GetFavoritedTime(b.WorldId, category)) * (inverted ? -1 : 1);
                    break;
            }
            list.Sort(comparison);
        }

        protected override IPickerElement WrapModel(StoredWorld model)
        {
            return new DbWorldAdapter(model);
        }

        protected internal override void RefreshFavButtons()
        { 
            // no fav buttons, do nothing
        }

        protected override void SearchButtonClicked()
        {
            BuiltinUiUtils.ShowInputPopup("Local Search (World)", "", InputField.InputType.Standard, false,
                "Search!", (s, list, arg3) =>
                {
                    SetSearchListHeaderAndScrollToIt("Search running...");
                    LastSearchRequest = s;
                    FavCatMod.Database.RunBackgroundWorldSearch(s, AcceptSearchResult);
                });
        }
    }
}