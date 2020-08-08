using System.Collections;
using EmojiPageButtons;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[assembly:MelonModInfo(typeof(EmojiPageButtonsMod), "Emoji Page Buttons", "1.0.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonModGame("VRChat", "VRChat")]

namespace EmojiPageButtons
{
    public class EmojiPageButtonsMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            ExpansionKitApi.RegisterWaitConditionBeforeDecorating(WaitAndRegisterEmojiButtons());
        }

        private IEnumerator WaitAndRegisterEmojiButtons()
        {
            while (QuickMenu.prop_QuickMenu_0 == null)
                yield return null;

            var emojiMenuRoot = QuickMenu.prop_QuickMenu_0.transform.Find("EmojiMenu");
            if (emojiMenuRoot == null)
            {
                MelonLogger.LogError("Emoji menu root not found");
                yield break;
            }

            var emojiMenu = emojiMenuRoot.GetComponent<EmojiMenu>();
            
            var storeGo = new GameObject("ClonedPageStore");
            storeGo.transform.SetParent(emojiMenu.transform);
            storeGo.SetActive(false);

            for (var index = 0; index < emojiMenu.pages.Count; index++)
            {
                var pageGo = emojiMenu.pages[index];

                var clone = new GameObject($"Page{index}Button", new []{Il2CppType.Of<RectTransform>()});
                clone.transform.SetParent(storeGo.transform, false);
                var grid = clone.AddComponent<GridLayoutGroup>();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.cellSize = new Vector2(33, 33);
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.constraintCount = 3;
                
                foreach (var buttonXformObject in pageGo.transform)
                {
                    var buttonTransform = buttonXformObject.Cast<Transform>();
                    if (!buttonTransform.gameObject.activeSelf) continue;

                    var buttonClone = Object.Instantiate(buttonTransform.gameObject, clone.transform, false);
                    CleanStuff(buttonClone);
                }

                var index1 = index;
                ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.EmojiQuickMenu, "", () =>
                {
                    emojiMenu.pages[emojiMenu.field_Private_Int32_0].SetActive(false);
                    pageGo.SetActive(true);
                    emojiMenu.field_Private_Int32_0 = index1;
                }, buttonGo =>
                {
                    Object.Instantiate(clone, buttonGo.transform, false);
                });
            }
        }

        private void CleanStuff(GameObject obj)
        {
            var compos = obj.GetComponents<Component>();
            foreach (var component in compos)
            {
                if (component.TryCast<Image>() != null || component.TryCast<Text>() != null)
                    continue;
                
                if (component.TryCast<Button>() != null || component.TryCast<MonoBehaviour>() != null)
                    Object.Destroy(component);
            }
            
            foreach (var o in obj.transform) 
                CleanStuff(o.Cast<Transform>().gameObject);
        }
    }
}