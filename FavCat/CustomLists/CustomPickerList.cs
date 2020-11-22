using System;
using System.Collections.Generic;
using System.Linq;
using FavCat.Database.Stored;
using MelonLoader;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace FavCat.CustomLists
{
    public class CustomPickerList : MonoBehaviour
    {
        private int myDisplayedRows = 1;
        private int myColumns = 5;
        
        private RectTransform myContentRoot;
        private RectTransform myRectTransform;
        private ScrollRect myContentScrollRect;
        
        private const int FullWidth = 1500; // 1526
        private const int AvatarWidth = 1139; // 1144
        private const int FullCellX = 305;
        private const int AvatarCellX = 285; // 310 is original cell X
        private const int CellY = 200;

        private Text myHeaderText;
        private Text myCountText;
        private Text myFavText;
        private Button myFavButton;

        private int myCellSize = FullCellX;

        private string myHeaderString;
        private string myFavButtonText;
        private bool myFavButtonEnabled;
        private bool myFavButtonVisible;

        public string HeaderString
        {
            [HideFromIl2Cpp]
            get => myHeaderString;
            [HideFromIl2Cpp]
            set
            {
                myHeaderString = value;
                if (myHeaderText != null) myHeaderText.text = value;
            }
        }

        public StoredCategory Category;

        private readonly List<IPickerElement> myModels = new List<IPickerElement>();
        
        private readonly Dictionary<(int X, int Y), GameObject> myPickersByCoordinate = new Dictionary<(int, int), GameObject>();
        
        public IReadOnlyList<IPickerElement> Models
        {
            [HideFromIl2Cpp]
            get => myModels;
        }

        private bool myIsAvatarListSizing;

        public Action? OnFavClick;
        public Action? OnSettingsClick;
        public Action<IPickerElement>? OnModelClick;
        public Action<int>? VisibleRowsChanged;

        [HideFromIl2Cpp]
        public CustomPickerList(IntPtr ptr) : base(ptr)
        {
        }

        [HideFromIl2Cpp]
        public void SetAvatarListSizing(bool isAvatarList)
        {
            myIsAvatarListSizing = isAvatarList;
            if (myRectTransform == null) return;
            var layoutElement = myRectTransform.GetComponent<LayoutElement>();
            var listWidth = isAvatarList ? AvatarWidth : FullWidth;
            layoutElement.minWidth = -1;
            myRectTransform.sizeDelta = new Vector2(listWidth, myRectTransform.sizeDelta.y);
            myCellSize = isAvatarList ? AvatarCellX : FullCellX;
            myColumns = isAvatarList ? 4 : 5;
            foreach (var child in myRectTransform)
            {
                var childXform = child.Cast<RectTransform>();
                childXform.sizeDelta = new Vector2(listWidth, childXform.sizeDelta.y);
            }
        }

        [HideFromIl2Cpp]
        public void SetFavButtonVisible(bool visible)
        {
            myFavButtonVisible = visible;
            if (myFavButton != null)
                myFavButton.gameObject.SetActive(visible);
        }

        [HideFromIl2Cpp]
        public void SetFavButtonText(string text, bool enabled)
        {
            myFavButtonText = text;
            myFavButtonEnabled = enabled;

            if (myFavText == null) return;
            
            myFavText.text = text;
            myFavButton.enabled = enabled;
        }

        public void Awake()
        {
            try
            {
                var transform = this.transform;
                myHeaderText = transform.Find("Header/ListLabel").GetComponent<Text>();
                myCountText = transform.Find("Header/PageLabel").GetComponent<Text>();
                myFavText = transform.Find("Header/FavButton/Text").GetComponent<Text>();
                myFavButton = transform.Find("Header/FavButton").GetComponent<Button>();

                myContentRoot = transform.Find("Scroll View/Viewport/ContentRoot").Cast<RectTransform>();
                myRectTransform = transform.Cast<RectTransform>();

                transform.Find("Header/LessButton").GetComponent<Button>().onClick.AddListener((Action) CollapseClick);
                transform.Find("Header/MoreButton").GetComponent<Button>().onClick.AddListener((Action) ExpandClick);
                transform.Find("Header/FavButton").GetComponent<Button>().onClick.AddListener((Action) FavClick);
                transform.Find("Header/SettingsButton").GetComponent<Button>().onClick.AddListener((Action) SettingsClick);

                transform.Find("Header/HomeButton").GetComponent<Button>().onClick.AddListener((Action) HomeClick);
                transform.Find("Header/EndButton").GetComponent<Button>().onClick.AddListener((Action) EndClick);

                myContentScrollRect = transform.Find("Scroll View").GetComponent<ScrollRect>();
                myContentScrollRect.onValueChanged.AddListener((Action<Vector2>) ScrollValueChanged);

                SetAvatarListSizing(myIsAvatarListSizing);
                DoResize();
                RecreatePickers();
                HeaderString = HeaderString;
                SetFavButtonText(myFavButtonText, myFavButtonEnabled);
                SetFavButtonVisible(myFavButtonVisible);
            }
            catch (Exception ex)
            {
                MelonLogger.LogError(ex.ToString());
            }
        }

        [HideFromIl2Cpp]
        private void ScrollValueChanged(Vector2 obj)
        {
            RecreatePickers();
        }

        [HideFromIl2Cpp]
        public void SetList<T>(IEnumerable<T> models, bool resetScroll) where T : class, IPickerElement
        {
            myModels.Clear();

            RemoveAllPickers();

            myModels.AddRange(models);

            DoResize();

            if (resetScroll && myContentScrollRect != null)
                myContentScrollRect.horizontalNormalizedPosition = 0f;

            RecreatePickers();
        }

        [HideFromIl2Cpp]
        private void RemoveAllPickers()
        {
            foreach (var keyValuePair in myPickersByCoordinate)
            {
                keyValuePair.Value.SetActive(false);
                keyValuePair.Value.GetComponent<CustomPicker>().Clean();
                PickerPool.Instance.Release(keyValuePair.Value);
            }
            
            myPickersByCoordinate.Clear();
        }


        [HideFromIl2Cpp]
        private void RecreatePickers()
        {
            if (myCountText == null || myContentScrollRect == null) return;
            if (myDisplayedRows == 0)
            {
                myCountText.text = $"{myModels.Count}";
                return;
            }

            var clampedNormalizedPosition = Mathf.Clamp01(myContentScrollRect.horizontalNormalizedPosition);
            var currentViewportStart = (int) (clampedNormalizedPosition * (myModels.Count - myDisplayedRows * myColumns + myDisplayedRows - 1) / myDisplayedRows - 1);
            if (currentViewportStart < 0) currentViewportStart = 0;

            var currentViewportEnd = currentViewportStart + myColumns + 2;

            if (myModels.Count <= myDisplayedRows * myColumns)
                myCountText.text = myModels.Count.ToString();
            else
                myCountText.text = $"{(int) (clampedNormalizedPosition * myModels.Count + 0.5f)} / {myModels.Count}";

            var pickersToRecycle = myPickersByCoordinate
                .Where(it => it.Key.X < currentViewportStart || it.Key.X > currentViewportEnd).ToList();
            
            foreach (var keyValuePair in pickersToRecycle)
            {
                myPickersByCoordinate.Remove(keyValuePair.Key);
                
                keyValuePair.Value.SetActive(false);
                keyValuePair.Value.GetComponent<CustomPicker>().Clean();
                PickerPool.Instance.Release(keyValuePair.Value);
            }

            for (var x = currentViewportStart; x <= currentViewportEnd; x++)
            {
                for (var y = 0; y < myDisplayedRows; y++)
                {
                    if(myPickersByCoordinate.ContainsKey((x, y)))
                        continue;
                    
                    var modelIndex = x * myDisplayedRows + y;
                    if (modelIndex >= myModels.Count)
                        continue;

                    var newPicker = PickerPool.Instance.Request();
                    var rectTransform = newPicker.transform.Cast<RectTransform>();
                    rectTransform.SetParent(myContentRoot, false);
                    rectTransform.sizeDelta = new Vector2(myCellSize, CellY);
                    rectTransform.anchoredPosition = new Vector2(x * myCellSize, (myDisplayedRows - y) * CellY);
                    newPicker.SetActive(true);

                    var model = myModels[modelIndex];
                    
                    InitPicker(model, newPicker);
                    myPickersByCoordinate[(x, y)] = newPicker;
                }
            }
        }

        [HideFromIl2Cpp]
        private void InitPicker(IPickerElement model, GameObject pickerGo)
        {
            var picker = pickerGo.GetComponent<CustomPicker>();
            picker.Initialize(model, _ => OnModelClick?.Invoke(model));
        }

        [HideFromIl2Cpp]
        private void DoResize()
        {
            if (myContentRoot == null)
                return;
            
            // y anchors are set to (0, 1) aka fill, so yDelta = 0 is normal
            myContentRoot.sizeDelta = new Vector2((myModels.Count + (myDisplayedRows - 1)) / Math.Max(1, myDisplayedRows) * myCellSize, 0);
            
            myRectTransform.GetComponent<LayoutElement>().minHeight = 58 + CellY * myDisplayedRows;
            
            myContentRoot.gameObject.SetActive(myDisplayedRows > 0);
            
            RemoveAllPickers();
        }

        [HideFromIl2Cpp]
        private void CollapseClick()
        {
            if (myDisplayedRows > 0)
                myDisplayedRows--;
            else
                return;

            VisibleRowsChanged?.Invoke(myDisplayedRows);
            
            DoResize();
            RecreatePickers();
        }

        [HideFromIl2Cpp]
        private void ExpandClick()
        {
            if (myDisplayedRows < 4)
                myDisplayedRows++;
            else
                return;

            VisibleRowsChanged?.Invoke(myDisplayedRows);
            
            DoResize();
            RecreatePickers();
        }

        [HideFromIl2Cpp]
        private void FavClick() => OnFavClick?.Invoke();
        [HideFromIl2Cpp]
        private void SettingsClick() => OnSettingsClick?.Invoke();
        
        [HideFromIl2Cpp]
        private void HomeClick()
        {
            myContentScrollRect.horizontalNormalizedPosition = 0f;
            
            RecreatePickers();
        }

        [HideFromIl2Cpp]
        private void EndClick()
        {
            myContentScrollRect.horizontalNormalizedPosition = 1f;
            
            RecreatePickers();
        }

        [HideFromIl2Cpp]
        public void SetVisibleRows(int i)
        {
            myDisplayedRows = i;

            if (myRectTransform == null) return;
            
            DoResize();
            RecreatePickers();
        }
    }
}