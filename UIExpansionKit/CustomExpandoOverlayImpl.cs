using System.Collections.Generic;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit
{
    internal class CustomExpandoOverlayImpl : CustomLayoutedPageWithOwnedMenuImpl
    {
        private static readonly Stack<CustomLayoutedPageWithOwnedMenuImpl> ourExpandoPagesStack = new();
        
        public CustomExpandoOverlayImpl(LayoutDescription? layoutDescription) : base(layoutDescription)
        {
            IsQuickMenu = true;
        }
        
        protected override Transform ParentTransform => UiExpansionKitMod.Instance.myQmExpandosRoot;
        protected override GameObject MenuPrefab => UiExpansionKitMod.Instance.StuffBundle.GenericPopupWindow;
        protected override Transform GetContentRoot(Transform instantiatedMenu) => instantiatedMenu.Find("Content/Scroll View/Viewport/Content");
        protected override RectTransform GetTopLevelUiObject(Transform instantiatedMenu) => instantiatedMenu.Cast<RectTransform>();
        protected override bool CloseOnMenuClose => true;
        protected override Stack<CustomLayoutedPageWithOwnedMenuImpl> PanelStack => ourExpandoPagesStack;

        protected override void AdjustMenuTransform(Transform transform, int layer)
        {
            transform.localScale = Vector3.one;
            transform.Find("Content/Background").GetComponent<Image>().MakeBackgroundMoreSolid();
            transform.Cast<RectTransform>().localPosition = new Vector3(0, 0, -3f + -1f * layer);
        }
    }
}