using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace UIExpansionKit.API
{
    public static class ExpansionKitApi
    {
        internal static readonly Dictionary<ExpandedMenu, CustomLayoutedPageImpl> ExpandedMenus = new Dictionary<ExpandedMenu, CustomLayoutedPageImpl>();
        internal static readonly Dictionary<string, GameObject> CustomCategoryUIs = new Dictionary<string, GameObject>();
        internal static readonly List<IEnumerator> ExtraWaitCoroutines = new List<IEnumerator>();

        internal static readonly Dictionary<(string, string), IList<(string SettingsValue, string DisplayName)>> EnumSettings = new Dictionary<(string, string), IList<(string SettingsValue, string DisplayName)>>();
        
        /// <summary>
        /// Register a simple button for given menu
        /// </summary>
        /// <param name="menu">Menu to attach this button to</param>
        /// <param name="text">User-visible button text</param>
        /// <param name="onClick">Button click action</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the button is instantiated</param>
        [Obsolete("Use GetExpandedMenu(menu).AddSimpleButton")]
        public static void RegisterSimpleMenuButton(ExpandedMenu menu, string text, Action onClick, Action<GameObject> instanceConsumer = null)
        {
            GetExpandedMenu(menu).AddSimpleButton(text, onClick, instanceConsumer);
        }

        /// <summary>
        /// Registers a custom button prefab. This prefab can be instantiated multiple times. 
        /// </summary>
        /// <param name="menu">Menu to attach this button to</param>
        /// <param name="gameObject">Button prefab</param>
        /// <param name="instanceConsumer">(optional) this action will be invoked when the prefab is instantiated</param>
        [Obsolete("Use GetExpandedMenu(menu).AddCustomButton")]
        public static void RegisterCustomMenuButton(ExpandedMenu menu, GameObject gameObject, Action<GameObject> instanceConsumer = null)
        {
            GetExpandedMenu(menu).AddCustomButton(gameObject, instanceConsumer);
        }

        /// <summary>
        /// Sets the specified GameObject as a prefab for given settings category. The prefab will be instantiated each time "Undo changes" is pressed, in addition to instantiating it on menu creation.
        /// </summary>
        /// <param name="categoryName">String passed as first parameter of <see cref="MelonPrefs.RegisterCategory"/></param>
        /// <param name="categoryUi">Prefab that acts as category settings UI</param>
        public static void RegisterCustomSettingsCategory(string categoryName, GameObject categoryUi)
        {
            if (CustomCategoryUIs.ContainsKey(categoryName))
            {
                MelonLogger.LogError($"Custom UI for category {categoryName} is already registered");
                return;
            }

            CustomCategoryUIs[categoryName] = categoryUi;
        }

        /// <summary>
        /// Returns the interface that can be used to add buttons to expanded menus
        /// </summary>
        /// <param name="menu">Existing menu that the expanded menu will be attached to</param>
        public static ICustomLayoutedMenu GetExpandedMenu(ExpandedMenu menu)
        {
            if (ExpandedMenus.TryGetValue(menu, out var result)) return result;
            
            return ExpandedMenus[menu] = new CustomLayoutedPageImpl(null);
        }

        internal class ButtonRegistration
        {
            public GameObject Prefab;
            
            public Action<GameObject> InstanceConsumer;
            
            public string Text;
            
            public Action Action;
            
            public Action<bool> ToggleAction;
            public Func<bool> InitialState;
        }

        /// <summary>
        /// Registers a coroutine that will be waited for before creating menu decorations.
        /// This can be used to delay decoration creation if your mod has async operations required to load custom UI prefabs.
        /// </summary>
        public static void RegisterWaitConditionBeforeDecorating(IEnumerator coroutine)
        {
            ExtraWaitCoroutines.Add(coroutine);
        }

        /// <summary>
        /// Registers a specific string-valued MelonPref as a enum value.
        /// In mod settings menu, this setting will be represented by a dropdown with specified possible values
        /// </summary>
        /// <param name="categoryName">MelonPrefs settings category</param>
        /// <param name="settingName">MelonPrefs setting name</param>
        /// <param name="possibleValues">A list of possible values</param>
        public static void RegisterSettingAsStringEnum(string categoryName, string settingName, IList<(string SettingsValue, string DisplayName)> possibleValues)
        {
            EnumSettings[(categoryName, settingName)] = possibleValues;
        }

        /// <summary>
        /// Registers a custom quick menu page.
        /// When shown, the page will be positioned above quick menu, overlapping the main 4x4 grid.
        /// </summary>
        /// <param name="requestedLayout">The layout of the page. If null, a custom layout is assumed - your mod code will need to assign sizes and positions to buttons manually</param>
        public static ICustomShowableLayoutedMenu CreateCustomQuickMenuPage(LayoutDescription? requestedLayout)
        {
            return new CustomQuickMenuPageImpl(requestedLayout);
        }

        /// <summary>
        /// Registers a custom full menu popup
        /// When shown, the popup will be positioned above full menu, approximately centered.
        /// </summary>
        /// <param name="requestedLayout">The layout of the popup. If null, a custom layout is assumed - your mod code will need to assign sizes and positions to buttons manually</param>
        public static ICustomShowableLayoutedMenu CreateCustomFullMenuPopup(LayoutDescription? requestedLayout)
        {
            return new CustomFullMenuPopupImpl(requestedLayout);
        }

        /// <summary>
        /// Hides all custom QM pages and full menu popups that are currently visible. Does not affect expanded menus.
        /// </summary>
        public static void HideAllCustomPopups()
        {
            CustomLayoutedPageWithOwnedMenuImpl.HideAll();
        }
    }
}