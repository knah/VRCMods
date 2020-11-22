using UIExpansionKit.API;
using UnityEngine;

namespace UIExpansionKit
{
    internal class CustomFullMenuPopupImpl : CustomLayoutedPageWithOwnedMenuImpl
    {
        public CustomFullMenuPopupImpl(LayoutDescription? layoutDescription) : base(layoutDescription)
        {
        }


        protected override Transform ParentTransform => GameObject.Find("UserInterface/MenuContent").transform;
        protected override GameObject MenuPrefab => UiExpansionKitMod.Instance.StuffBundle.GenericPopupWindow;
        protected override Transform GetContentRoot(Transform instantiatedMenu) => instantiatedMenu.Find("Content/Scroll View/Viewport/Content");
        protected override RectTransform GetTopLevelUiObject(Transform instantiatedMenu) => instantiatedMenu.Cast<RectTransform>();

        protected override void AdjustMenuTransform(Transform instantiatedMenu, int layer)
        {
            instantiatedMenu.localScale = Vector3.one * 2;
            instantiatedMenu.localPosition = new Vector3(-775 * 0, 435, -15 + -5 * layer);
        }
    }
}