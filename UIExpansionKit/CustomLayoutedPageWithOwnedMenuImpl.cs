using System.Collections.Generic;
using UIExpansionKit.API;
using UnityEngine;
using VRCSDK2;

namespace UIExpansionKit
{
    internal abstract class CustomLayoutedPageWithOwnedMenuImpl : CustomLayoutedPageImpl, ICustomShowableLayoutedMenu
    {
        private static readonly Stack<CustomLayoutedPageWithOwnedMenuImpl> ourCurrentlyVisibleMenus = new Stack<CustomLayoutedPageWithOwnedMenuImpl>();
        
        public CustomLayoutedPageWithOwnedMenuImpl(LayoutDescription? layoutDescription) : base(layoutDescription)
        {
        }

        protected abstract Transform ParentTransform { get; }
        protected abstract GameObject MenuPrefab { get; }
        protected abstract Transform GetContentRoot(Transform instantiatedMenu);
        protected abstract RectTransform GetTopLevelUiObject(Transform instantiatedMenu);
        protected abstract void AdjustMenuTransform(Transform instantiatedMenu, int layer);
        protected virtual bool CloseOnMenuClose { get; }

        private GameObject myMenuInstance;

        public void Show(bool onTop = false)
        {
            if (myMenuInstance != null) 
                return;

            if (!onTop)
            {
                while (ourCurrentlyVisibleMenus.Count > 0) 
                    ourCurrentlyVisibleMenus.Pop().Hide();
            }

            var newInstance = Object.Instantiate(MenuPrefab, ParentTransform);
            UiExpansionKitMod.SetLayerRecursively(newInstance, QuickMenu.prop_QuickMenu_0.gameObject.layer);
            AdjustMenuTransform(newInstance.transform, ourCurrentlyVisibleMenus.Count + 1);
            var topLevelTransform = GetTopLevelUiObject(newInstance.transform);
            if (LayoutDescription != null)
            {
                var oldPosition = topLevelTransform.anchoredPosition;
                topLevelTransform.sizeDelta = new Vector2(topLevelTransform.sizeDelta.x, 5 + LayoutDescription.Value.NumRows * (5 + LayoutDescription.Value.RowHeight));
                topLevelTransform.anchoredPosition = oldPosition;
            }

            newInstance.GetComponentInChildren<Canvas>().gameObject.AddComponent<VRC_UiShape>();
            if (CloseOnMenuClose)
            {
                BuiltinUiUtils.QuickMenuClosed += Hide;
                BuiltinUiUtils.FullMenuClosed += Hide;
            }

            var contentRoot = GetContentRoot(newInstance.transform);
            PopulateButtons(contentRoot.transform, IsQuickMenu, true);

            myMenuInstance = newInstance;
            ourCurrentlyVisibleMenus.Push(this);
        }

        public void Hide()
        {
            if (myMenuInstance == null) 
                return;
            
            while (ourCurrentlyVisibleMenus.Count > 0)
            {
                var topObject = ourCurrentlyVisibleMenus.Pop();
                if (ReferenceEquals(topObject, this))
                    break;
                    
                topObject.Hide();
            }

            if (CloseOnMenuClose)
            {
                BuiltinUiUtils.QuickMenuClosed -= Hide;
                BuiltinUiUtils.FullMenuClosed -= Hide;
            }

            Object.Destroy(myMenuInstance);
            myMenuInstance = null;
        }

        public static void HideAll()
        {
            while (ourCurrentlyVisibleMenus.Count > 0) 
                ourCurrentlyVisibleMenus.Pop().Hide();
        }
    }
}