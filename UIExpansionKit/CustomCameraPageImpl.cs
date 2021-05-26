using UIExpansionKit.API;
using UnityEngine;

namespace UIExpansionKit
{
    internal class CustomCameraPageImpl : CustomLayoutedPageWithOwnedMenuImpl
    {
        public CustomCameraPageImpl(LayoutDescription? layoutDescription) : base(layoutDescription)
        {
            IsQuickMenu = true;
        }
        
        protected override Transform ParentTransform => UiExpansionKitMod.Instance.myCameraExpandoRoot.Find("Content");
        protected override GameObject MenuPrefab => UiExpansionKitMod.Instance.StuffBundle.GenericPopupWindow;
        protected override Transform GetContentRoot(Transform instantiatedMenu) => instantiatedMenu.Find("Content/Scroll View/Viewport/Content");
        protected override RectTransform GetTopLevelUiObject(Transform instantiatedMenu) => instantiatedMenu.Cast<RectTransform>();
        protected override bool CloseOnMenuClose => false;

        protected override void AdjustMenuTransform(Transform transform, int layer)
        {
            transform.localScale = Vector3.one;
            transform.Cast<RectTransform>().localPosition = new Vector3(0, 0, -0.003f + -0.001f * layer);
            
            UiExpansionKitMod.SetLayerRecursively(transform.gameObject, 17);
        }
    }
}