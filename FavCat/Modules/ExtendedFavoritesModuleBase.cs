using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FavCat.Adapters;
using FavCat.CustomLists;
using FavCat.Database;
using FavCat.Database.Stored;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FavCat.Modules
{
    public abstract class ExtendedFavoritesModuleBase<T> where T: class, INamedStoredObject
    {
        protected const string SearchCategoryName = "Local search results";
        protected const string ExpandEnforcerGameObjectName = "ExpandEnforcer";
        
        protected readonly Dictionary<string, CustomPickerList> PickerLists = new Dictionary<string, CustomPickerList>();
        protected readonly CustomPickerList SearchList;
        protected readonly Transform listsParent;
        private readonly bool myHasUpdateAndCreationDates;
        protected readonly DatabaseFavoriteHandler<T> Favorites;
        private readonly ExpandedMenu myExpandedMenu;

        protected string LastSearchRequest = "";

        private List<T>? mySearchResult;
        private StoredCategory myCurrentlySelectedCategory;
        private CustomPickerList myCurrentlySelectedList;

        private UISoundCollection mySoundCollection;

        protected void PlaySound()
        {
            if (!FavCatSettings.DoClickSounds) return;
            var soundPlayer = VRCUiSoundPlayer.field_Private_Static_VRCUiSoundPlayer_0;
            if (mySoundCollection == null || mySoundCollection.Click == null || soundPlayer == null) return;

            soundPlayer.field_Private_AudioSource_0.PlayOneShot(mySoundCollection.Click, 1f);
        }
        
        
        protected abstract void OnPickerSelected(IPickerElement picker);
        protected abstract void OnFavButtonClicked(StoredCategory storedCategory);
        protected abstract bool FavButtonsOnLists { get; }
        protected abstract void SortModelList(string sortCriteria, string category, List<(StoredFavorite?, T)> list);
        protected abstract IPickerElement WrapModel(StoredFavorite? favorite, T model);
        protected internal abstract void RefreshFavButtons();
        protected abstract void SearchButtonClicked();

        protected bool CanPerformAdditiveActions { get; }
        protected bool CanShowExistingLists { get; }

        public virtual void ShowAnnoyingMessage()
        {
        }


        protected ExtendedFavoritesModuleBase(ExpandedMenu expandedMenu, DatabaseFavoriteHandler<T> favoriteHandler, Transform listsParent, bool canPerformAdditiveActions, bool canShowExistingLists, bool hasUpdateAndCreationDates = true)
        {
            myExpandedMenu = expandedMenu;
            Favorites = favoriteHandler;
            CanPerformAdditiveActions = canPerformAdditiveActions;
            CanShowExistingLists = canShowExistingLists;

            ExpansionKitApi.GetExpandedMenu(myExpandedMenu).AddSimpleButton("Local Search", SearchButtonClicked);
            if (CanPerformAdditiveActions) ExpansionKitApi.GetExpandedMenu(myExpandedMenu).AddSimpleButton("New Category", CreateCategory);
            ExpansionKitApi.GetExpandedMenu(myExpandedMenu).AddSimpleButton("More FavCat...", ShowExtraOptionsMenu);
            
            this.listsParent = listsParent;
            myHasUpdateAndCreationDates = hasUpdateAndCreationDates;

            if (CanShowExistingLists)
            {
                var knownCategories = Favorites.GetCategories().ToList();
                if (knownCategories.Count == 0)
                {
                    var newCategory = new StoredCategory {CategoryName = "Local Favorites", SortType = "!added"};
                    Favorites.UpdateCategory(newCategory);
                    CreateList(newCategory);
                }
                else
                    foreach (var categoryName in knownCategories)
                        if (categoryName.CategoryName != SearchCategoryName)
                            CreateList(categoryName);
            }
            
            var searchCategory = Favorites.GetCategory(SearchCategoryName) ??
                                 new StoredCategory {CategoryName = SearchCategoryName, SortType = hasUpdateAndCreationDates ? "!updated" : "name"};
            Favorites.UpdateCategory(searchCategory);
            
            Favorites.OnCategoryContentsChanged += categoryName =>
            {
                if (PickerLists.TryGetValue(categoryName, out var list))
                {
                    UpdateListElements(categoryName, list);
                    
                    RefreshFavButtons();
                }
            };
            
            ReorderLists();

            SearchList = FavCatMod.Instance.CreateCustomList(this.listsParent);
            SearchList.SetAvatarListSizing(myExpandedMenu == ExpandedMenu.AvatarMenu);
            SearchList.HeaderString = SearchCategoryName;
            SearchList.SetFavButtonText("Clear", true);
            SearchList.SetFavButtonVisible(true);
            SearchList.OnSettingsClick += () =>
            {
                myCurrentlySelectedList = SearchList;
                myCurrentlySelectedCategory = searchCategory;
                
                ShowListSettingsMenu(searchCategory);
            };
            SearchList.OnFavClick += () =>
            {
                SearchList.SetList(Enumerable.Empty<IPickerElement>(), true);
                SearchList.SetVisibleRows(0);
            };
            SearchList.VisibleRowsChanged += newRows =>
            {
                searchCategory.VisibleRows = newRows;
                Favorites.UpdateCategory(searchCategory);
            };
            SearchList.Category = searchCategory;
            SearchList.SetList(Enumerable.Empty<IPickerElement>(), true);
            SearchList.SetVisibleRows(searchCategory.VisibleRows);
            SearchList.OnModelClick += OnPickerSelected;
            SearchList.transform.SetSiblingIndex(this.listsParent.transform.childCount);

            // assign these to something random by default - 
            myCurrentlySelectedCategory = searchCategory;
            myCurrentlySelectedList = SearchList;

            listsParent.GetOrAddComponent<EnableDisableListener>().OnEnabled += () =>
            {   // worlds menu does something fishy with lists - just fix our ordering on each menu open, it's fast, I promise
                MelonCoroutines.Start(ReorderListsAfterDelay());
            };

            // hide custom lists if original search list is showing
            var originalSearchList = GatherLists().FirstOrDefault(it => it.ListName.ToLower() == "search results" && !it.IsCustom);
            if (originalSearchList.ListTransform)
            {
                var listener = originalSearchList.ListTransform.GetOrAddComponent<EnableDisableListener>();
                listener.OnEnabled += OnSearchListShown;
                listener.OnDisabled += OnSearchListHidden;
            }

            mySoundCollection = GameObject.Find("/UserInterface/QuickMenu/CameraMenu/BackButton")
                .GetComponent<ButtonReaction>().field_Public_UISoundCollection_0;
        }

        private void ShowListSortingMenu()
        {
            var myListSortingMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            myListSortingMenu.AddSimpleButton("Name (ascending)", () => UpdateSelectedListSort("name", myListSortingMenu));
            myListSortingMenu.AddSimpleButton("Name (descending)", () => UpdateSelectedListSort("!name", myListSortingMenu));

            if (myCurrentlySelectedCategory.CategoryName != SearchCategoryName)
            {
                myListSortingMenu.AddSimpleButton("Date favorited (new first)", () => UpdateSelectedListSort("!added", myListSortingMenu));
                myListSortingMenu.AddSimpleButton("Date favorited (old first)", () => UpdateSelectedListSort("added", myListSortingMenu));
            }

            if (myHasUpdateAndCreationDates)
            {
                myListSortingMenu.AddSimpleButton("Date updated (new first)", () => UpdateSelectedListSort("!updated", myListSortingMenu));
                myListSortingMenu.AddSimpleButton("Date updated (old first)", () => UpdateSelectedListSort("updated", myListSortingMenu));

                myListSortingMenu.AddSimpleButton("Date created (new first)", () => UpdateSelectedListSort("!created", myListSortingMenu));
                myListSortingMenu.AddSimpleButton("Date created (old first)", () => UpdateSelectedListSort("created", myListSortingMenu));
            }

            myListSortingMenu.AddSpacer();
            myListSortingMenu.AddSimpleButton("Cancel", myListSortingMenu.Hide);
            
            myListSortingMenu.Show(true);
        }

        private void OnSearchListShown()
        {
            foreach (var customPickerList in PickerLists) customPickerList.Value.gameObject.SetActive(false);
        }

        private void OnSearchListHidden()
        {
            foreach (var customPickerList in PickerLists) customPickerList.Value.gameObject.SetActive(true);
        }

        private IEnumerator ReorderListsAfterDelay()
        {
            yield return null;
            ReorderLists();
            yield return new WaitForSeconds(2f);
            ReorderLists();
        }
        
        private void CreateCategory()
        {
            if (!CanPerformAdditiveActions)
            {
                ShowAnnoyingMessage();
                return;
            }
            
            BuiltinUiUtils.ShowInputPopup("Enter category name", "", InputField.InputType.Standard, false, "Create",
                (s, _, __) =>
                {
                    var existingCategory = Favorites.GetCategory(s);
                    if (existingCategory == null)
                    {
                        var newCategory = new StoredCategory
                        {
                            CategoryName = s, SortType = "!added", VisibleRows = 1
                        };
                        Favorites.UpdateCategory(newCategory);
                        CreateList(newCategory);
                        ReorderLists();
                        RefreshFavButtons();
                    }
                    else
                    {
                        var messagePopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                        messagePopup.AddLabel("Category with that name already exists");
                        for (var i = 0; i < 6; i++)
                            messagePopup.AddSpacer();
                        messagePopup.AddSimpleButton("Close", () => messagePopup.Hide());
                        messagePopup.Show(true);
                    }
                });
        }

        internal virtual void Update()
        {
            if (mySearchResult != null)
                ProcessSearchResults();
        }
        
        internal void ReorderLists()
        {
            var storedOrderFull = Favorites.GetStoredOrder();
            var storedOrder = storedOrderFull.Order;
            Transform? lastSeenList;
            int lastSeenListIndex = -1;

            var knownLists = new Dictionary<String, (Transform ListTransform, string ListName, bool IsCustom)>();
            foreach (var list in GatherLists())
            {
                if (Imports.IsDebugMode() && knownLists.ContainsKey(list.ListName))
                    MelonLogger.Log($"List {list.ListName} is duplicated");
                
                knownLists[list.ListName] = list;
            }
            
            foreach (var categoryInfo in storedOrder)
            {
                if (categoryInfo.IsExternal)
                {
                    // litedb likes saving "" as null by default
                    if (knownLists.TryGetValue(categoryInfo.Name ?? "", out var existingList))
                    {
                        lastSeenList = existingList.Item1;
                        lastSeenListIndex = lastSeenList.GetSiblingIndex();
                    }
                }

                if (!PickerLists.TryGetValue(categoryInfo.Name ?? "", out var list))
                    continue;

                lastSeenList = list.transform;
                lastSeenList.SetSiblingIndex2(lastSeenListIndex + 1);
                lastSeenListIndex = lastSeenList.GetSiblingIndex();
            }
            
            foreach (var listToHideName in storedOrderFull.DefaultListsToHide)
                if (knownLists.TryGetValue(listToHideName ?? "", out var listToHide) && !listToHide.IsCustom)
                    listToHide.ListTransform.gameObject.SetActive(false);
        }

        protected void ShowListSettingsMenu(StoredCategory category)
        {
            var listSettingsMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.QuickMenu3Columns);
            
            listSettingsMenu.AddSimpleButton("Rename", RenameSelectedList);
            listSettingsMenu.AddSimpleButton($"Move", MoveSelectedList);
            listSettingsMenu.AddSimpleButton($"Sort (current: {HumanSort(category)}", ShowListSortingMenu);

            listSettingsMenu.AddSpacer();
            listSettingsMenu.AddSpacer();
            listSettingsMenu.AddSpacer();

            if (category.CategoryName == SearchCategoryName)
                if (CanPerformAdditiveActions)
                    listSettingsMenu.AddSimpleButton("Save as new category", () => SaveSearchAsCategory());
                else
                    listSettingsMenu.AddSpacer();
            else
                listSettingsMenu.AddSimpleButton("Delete", DeleteSelectedList);
            
            listSettingsMenu.AddSpacer();
            listSettingsMenu.AddSimpleButton("Back", listSettingsMenu.Hide);
            
            listSettingsMenu.Show();
        }

        private static void ShowBigListWarning(int listSize, Action onYes)
        {
            var confirmMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            confirmMenu.AddLabel($"The list you're trying to create has {listSize} elements");
            confirmMenu.AddLabel("This will likely incur a lag spike when the list is created and on startup");
            confirmMenu.AddSpacer();
            confirmMenu.AddLabel("Do you want to continue?");
            confirmMenu.AddSpacer();
            confirmMenu.AddSimpleButton("Sure! Lag away!", () =>
            {
                confirmMenu.Hide();
                onYes();
            });
            confirmMenu.AddSpacer();
            confirmMenu.AddSimpleButton("Nooo no lags!", () => confirmMenu.Hide());
            
            confirmMenu.Show(true);
        }

        private static string HumanSort(StoredCategory category)
        {
            if (category.SortType.StartsWith("!"))
                return category.SortType.Substring(1) + ", descending";
            
            return category.SortType + ", ascending";
        }

        private void DeleteSelectedList()
        {
            var currentList = myCurrentlySelectedCategory.CategoryName;
            var confirmMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            confirmMenu.AddLabel($"Are you sure you want to delete favorite list '{currentList}'?");
            confirmMenu.AddLabel($"It, and all favorites in it, will be lost forever (a very long time)!");
            confirmMenu.AddSpacer();
            confirmMenu.AddSpacer();
            confirmMenu.AddSimpleButton($"Delete '{currentList}'", DeleteSelectedListConfirmed);
            confirmMenu.AddSpacer();
            confirmMenu.AddSpacer();
            confirmMenu.AddSimpleButton($"Cancel", () => confirmMenu.Hide());
            confirmMenu.Show(true);
        }

        private void DeleteSelectedListConfirmed()
        {
            PickerLists.Remove(myCurrentlySelectedCategory.CategoryName);
            Object.Destroy(myCurrentlySelectedList.gameObject);
            
            Favorites.DeleteCategory(myCurrentlySelectedCategory);
            
            ExpansionKitApi.HideAllCustomPopups();
        }

        private void SaveSearchAsCategory(bool confirmed = false)
        {
            ExpansionKitApi.HideAllCustomPopups();
            
            if (SearchList.Models.Count > 1000 && !confirmed)
            {
                ShowBigListWarning(SearchList.Models.Count, () => SaveSearchAsCategory(true));
                return;
            }

            BuiltinUiUtils.ShowInputPopup($"Enter a name for saved search results", "Saved " + SearchList.HeaderString, InputField.InputType.Standard, false, "Create!",
                (s, _, __) =>
                {
                    var existingCategory = Favorites.GetCategory(s);
                    if (existingCategory == null)
                    {
                        var newCategory = new StoredCategory
                        {
                            CategoryName = s,
                            SortType = SearchList.Category.SortType
                        };
                        Favorites.UpdateCategory(newCategory);
                        FavoriteAll(newCategory).NoAwait();
                    }
                    else
                    {
                        var messagePopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                        messagePopup.AddLabel("Category with that name already exists");
                        for (var i = 0; i < 6; i++)
                            messagePopup.AddSpacer();
                        messagePopup.AddSimpleButton("Close", () => messagePopup.Hide());
                        messagePopup.Show(true);
                    }
                });
        }

        private async Task FavoriteAll(StoredCategory newCategory)
        {
            await Task.Run(() => { }).ConfigureAwait(false);
            foreach (var adapter in SearchList.Models)
                Favorites.AddFavorite(adapter.Id, newCategory.CategoryName);
            await FavCatMod.YieldToMainThread();
            CreateList(newCategory);
            ReorderLists();
            RefreshFavButtons();
        }

        internal void CreateList(StoredCategory storedCategory)
        {
            var list = FavCatMod.Instance.CreateCustomList(listsParent);
            list.HeaderString = storedCategory.CategoryName;
            list.SetAvatarListSizing(myExpandedMenu == ExpandedMenu.AvatarMenu);
            list.SetVisibleRows(storedCategory.VisibleRows);
            list.OnModelClick += OnPickerSelected;
            list.Category = storedCategory;
            list.OnFavClick += () => OnFavButtonClicked(storedCategory);
            list.SetFavButtonVisible(FavButtonsOnLists);
            list.OnSettingsClick += () =>
            {
                myCurrentlySelectedList = list;
                myCurrentlySelectedCategory = storedCategory;
                
                ShowListSettingsMenu(storedCategory);
            };
            list.VisibleRowsChanged += newRows =>
            {
                storedCategory.VisibleRows = newRows;
                Favorites.UpdateCategory(storedCategory);
            };
            UpdateListElements(storedCategory.CategoryName, list);
            list.transform.SetAsFirstSibling();

            var parentScroll = listsParent.GetComponentsInParent<ScrollRect>(true).FirstOrDefault();
            if (parentScroll != null)
                list.SetParentScrollRect(parentScroll);

            PickerLists[storedCategory.CategoryName] = list;
        }

        private void RenameSelectedList()
        {
            ExpansionKitApi.HideAllCustomPopups();
            
            BuiltinUiUtils.ShowInputPopup($"Enter new name for '{myCurrentlySelectedCategory.CategoryName}'", "", InputField.InputType.Standard, false, "Rename!",
                (s, _, __) =>
                {
                    var existingCategory = Favorites.GetCategory(s);
                    if (existingCategory == null)
                    {
                        PickerLists[s] = PickerLists[myCurrentlySelectedCategory.CategoryName];
                        PickerLists.Remove(myCurrentlySelectedCategory.CategoryName);
                        Favorites.RenameCategory(myCurrentlySelectedCategory, s);
                        myCurrentlySelectedList.HeaderString = s;
                    }
                    else
                    {
                        var messagePopup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
                        messagePopup.AddLabel("Category with that name already exists");
                        for (var i = 0; i < 6; i++)
                            messagePopup.AddSpacer();
                        messagePopup.AddSimpleButton("Close", () => messagePopup.Hide());
                        messagePopup.Show(true);
                    }
                });
        }

        private void MoveSelectedList()
        {
            ExpansionKitApi.HideAllCustomPopups();
            
            var moveOptionsMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            moveOptionsMenu.AddSimpleButton("The beginning of the list", () => UpdateSelectedListOrder(0));
            
            foreach (var subListObj in listsParent)
            {
                var subList = subListObj.Cast<Transform>();
                if (!subList.gameObject.activeSelf)
                    continue;
                var listName = ExtractListName(subList.gameObject);
                if (listName != null)
                    moveOptionsMenu.AddSimpleButton($"After '{listName}'", () => UpdateSelectedListOrder(subList.GetSiblingIndex() + 1));
            }
            
            moveOptionsMenu.AddSimpleButton("The end of the list", () => UpdateSelectedListOrder(listsParent.childCount));
            moveOptionsMenu.AddSpacer();
            moveOptionsMenu.AddSimpleButton("Cancel", moveOptionsMenu.Hide);
            
            moveOptionsMenu.Show();
        }
        
        private static string? ExtractListName(GameObject listRoot)
        {
            var customList = listRoot.GetComponent<CustomPickerList>();
            if (customList) return customList.Category.CategoryName;

            var avatarList = listRoot.GetComponent<UiAvatarList>();
            if (avatarList && avatarList.field_Public_EnumNPublicSealedvaInPuMiFaSpClPuLiCrUnique_0 == UiAvatarList.EnumNPublicSealedvaInPuMiFaSpClPuLiCrUnique.PublicQuest) 
                return null; // nobody likes this one

            return listRoot.transform.Find("Button/TitleText")?.GetComponent<Text>()?.text?.StripParenthesis() ?? listRoot.name;
        }

        private List<(Transform ListTransform, string ListName, bool IsCustom)> GatherLists()
        {
            var result = new List<(Transform, string, bool)>();
            
            foreach (var subListObj in listsParent)
            {
                var subList = subListObj.Cast<Transform>();
                
                if (subList.gameObject.name == ExpandEnforcerGameObjectName)
                    continue;

                var listName = ExtractListName(subList.gameObject);
                if (listName != null)
                    result.Add((subList, listName, (bool) subList.GetComponent<CustomPickerList>()));
            }

            return result;
        }
        
        private void UpdateSelectedListOrder(int position)
        {
            myCurrentlySelectedList.transform.SetSiblingIndex2(position);

            var oldOrder = Favorites.GetStoredOrder();
            Favorites.SetStoredOrder(GatherLists()
                .Select(it => new CategoryInfo {Name = it.ListName, IsExternal = !it.IsCustom}).ToList(), oldOrder.DefaultListsToHide);
            
            ExpansionKitApi.HideAllCustomPopups();
        }

        protected void UpdateListElements(string categoryName, CustomPickerList list, bool reuseList = false)
        {
            List<(StoredFavorite Favorite, T Model)> favs = reuseList
                ? list.Models.OfType<IStoredModelAdapter<T>>().Select(it => (it.StoredFavorite, it.Model)).ToList()
                : Favorites.ListFavorites(categoryName).ToList();
            SortModelList(list.Category.SortType, categoryName, favs);
            list.SetList(favs.Select(it => WrapModel(it.Favorite, it.Model)), false);
        }

        protected List<StoredCategory> GetCategoriesInSortedOrder()
        {
            var categories = Favorites.GetCategories().ToList();
            var categoryOrder = new Dictionary<string, int>();
            var list = Favorites.GetStoredOrder().Order;
            for (var i = 0; i < list.Count; i++) 
                categoryOrder[list[i].Name ?? ""] = i;

            categories.Sort((a, b) =>
                categoryOrder.GetOrDefault(a.CategoryName, Int32.MaxValue)
                    .CompareTo(categoryOrder.GetOrDefault(b.CategoryName, Int32.MaxValue)));

            return categories;
        }

        protected void AcceptSearchResult(IEnumerable<T> result)
        {
            mySearchResult = result.ToList();
        }

        protected void ProcessSearchResults()
        {
            var results = mySearchResult?.Select(it => ((StoredFavorite?) null, it)).ToList();
            mySearchResult = null;
            if (results == null) return;
            
            MelonLogger.Log("Local search done, {0} results", results.Count);

            SortModelList(SearchList.Category.SortType, SearchCategoryName, results);
            SearchList.SetList(results.Select(it => WrapModel(null, it.it)), true);
            
            SetSearchListHeaderAndScrollToIt($"Search results ({LastSearchRequest})");
        }

        protected void SetSearchListHeaderAndScrollToIt(string text)
        {
            SearchList.HeaderString = text;
            
            SearchList.SetVisibleRows(4);
            MelonCoroutines.Start(ScrollDownAfterDelay());
        }

        protected IEnumerator ScrollDownAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            listsParent.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
        
        private void UpdateSelectedListSort(string sort, IShowableMenu menuToHide)
        {
            myCurrentlySelectedCategory.SortType = sort;
            Favorites.UpdateCategory(myCurrentlySelectedCategory);
            UpdateListElements(myCurrentlySelectedCategory.CategoryName, myCurrentlySelectedList, true);
            
            menuToHide.Hide();
        }

        private void ShowExtraOptionsMenu()
        {
            var customMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            
            customMenu.AddSimpleButton("Hide default lists...", () =>
            {
                customMenu.Hide();
                ShowListHideMenu();
            });
            
            customMenu.AddSpacer();
            
            if (ImportFolderProcessor.ImportRunning)
                customMenu.AddLabel(ImportFolderProcessor.ImportStatusOuter + "\n" + ImportFolderProcessor.ImportStatusInner);
            else
                customMenu.AddSimpleButton(CanPerformAdditiveActions ? "Import databases and text files" : "Import databases", () =>
                {
                    customMenu.Hide();
                    ImportFolderProcessor.ProcessImportsFolder().NoAwait();
                });
            
            if (Favorites.EntityType == DatabaseEntity.Avatar)
                customMenu.AddSimpleButton("Show avatar favorites deprecation message", ShowAnnoyingMessage);
            else
                customMenu.AddSpacer();
            
            customMenu.AddSimpleButton("Open documentation in browser", () =>
            {
                customMenu.Hide();
                Process.Start("https://github.com/knah/VRCMods#favcat");
            });
            
            if (ReFetchFavoritesProcessor.ImportRunning)
                customMenu.AddLabel(ReFetchFavoritesProcessor.ImportStatusOuter + " " + ReFetchFavoritesProcessor.ImportStatusInner);
            else
                customMenu.AddSimpleButton("Re-fetch favorites", () =>
                {
                    customMenu.Hide();
                    ReFetchFavoritesProcessor.ReFetchFavorites().NoAwait();
                });
            
            if (ExportProcessor.IsExportingFavorites)
                customMenu.AddLabel($"Exporting: {ExportProcessor.ProcessedCategories} / {ExportProcessor.TotalCategories}");
            else
                customMenu.AddSimpleButton("Export favorites",
                    () =>
                    {
                        customMenu.Hide();
                        ExportProcessor.DoExportFavorites(Favorites).NoAwait();
                    });
            
            customMenu.AddSimpleButton("Close", customMenu.Hide);
            
            customMenu.Show();
        }

        private void ShowListHideMenu()
        {
            var customShowHideMenu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            var currentData = Favorites.GetStoredOrder();
            var currentHiddenLists = currentData.DefaultListsToHide.ToHashSet();
            
            foreach (var gatherList in GatherLists())
            {
                if (gatherList.IsCustom)
                    continue;

                var go = gatherList.ListTransform.gameObject;
                if (!go.activeSelf && !currentHiddenLists.Contains(gatherList.ListName))
                    continue; //it's already hidden by game itself

                Text buttonText = null;
                customShowHideMenu.AddSimpleButton($"{(currentHiddenLists.Contains(gatherList.ListName) ? "Show" : "Hide")} {gatherList.ListName}",
                    () =>
                    {
                        go.SetActive(!go.activeSelf);
                        if (go.activeSelf)
                            currentHiddenLists.Remove(gatherList.ListName);
                        else
                            currentHiddenLists.Add(gatherList.ListName);
                        
                        Favorites.SetStoredOrder(currentData.Order, currentHiddenLists.ToList());

                        buttonText!.text = $"{(currentHiddenLists.Contains(gatherList.ListName) ? "Show" : "Hide")} {gatherList.ListName}";
                    }, btn => buttonText = btn.GetComponentInChildren<Text>());
            }
            
            customShowHideMenu.AddSimpleButton("Close", customShowHideMenu.Hide);
            
            customShowHideMenu.Show();
        }
    }
}