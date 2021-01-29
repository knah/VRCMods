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
                myPageWorldInfo.transform.Find("WorldButtons/NewButton").GetComponent<Button>().interactable = true;
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

        private string myLastRequestedWorld = "";
        protected override void OnPickerSelected(IPickerElement picker)
        {
            if (picker.Id == myLastRequestedWorld) 
                return;

            myLastRequestedWorld = picker.Id;
            var world = new ApiWorld {id = picker.Id};
            world.Fetch(new Action<ApiContainer>(_ =>
            {
                myLastRequestedWorld = "";
                UiWorldList.Method_Public_Static_Void_ApiWorld_0(world);
            }), new Action<ApiContainer>(c =>
            {
                myLastRequestedWorld = "";
                if (Imports.IsDebugMode())
                    MelonLogger.Log("API request errored with " + c.Code + " - " + c.Error);
                if (c.Code == 404)
                {
                    FavCatMod.Database.CompletelyDeleteWorld(picker.Id);
                    var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddLabel("This world is not available anymore (deleted)");
                    menu.AddLabel("It has been removed from all favorite lists");
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSpacer();
                    menu.AddSimpleButton("Close", menu.Hide);
                    menu.Show();
                }
            }));
        }

        protected override void OnFavButtonClicked(StoredCategory storedCategory)
        {
            throw new NotSupportedException(); // they aren't clicked for worlds
        }

        protected override bool FavButtonsOnLists => false;
        protected override void SortModelList(string sortCriteria, string category, List<(StoredFavorite?, StoredWorld)> avatars)
        {
            var inverted = sortCriteria.Length > 0 && sortCriteria[0] == '!';
            Comparison<(StoredFavorite? Fav, StoredWorld Model)> comparison;
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

        protected override IPickerElement WrapModel(StoredFavorite? favorite, StoredWorld model) => new DbWorldAdapter(model, favorite);

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