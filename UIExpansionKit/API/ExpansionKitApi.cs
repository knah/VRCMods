using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace UIExpansionKit.API
{
    public static class ExpansionKitApi
    {
        internal static readonly Dictionary<ExpandedMenu, CustomLayoutedPageImpl> ExpandedMenus = new();
        internal static readonly Dictionary<string, GameObject> CustomCategoryUIs = new();
        internal static readonly Dictionary<string, CustomLayoutedPageImpl> SettingPageExtensions = new();
        internal static readonly List<IEnumerator> ExtraWaitCoroutines = new();
        internal static bool CanAddWaitCoroutines = true;

        internal static readonly Dictionary<(string, string), IList<(string SettingsValue, string DisplayName)>> EnumSettings = new();
        internal static readonly Dictionary<(string, string), SettingVisibilityRegistrationValue> SettingsVisibilities = new();

        internal static List<Action> onUiManagerInitDelegateList = new();

        internal class SettingVisibilityRegistrationValue
        {
            internal readonly Func<bool> IsVisible;
            internal event Action OnUpdateVisibility;

            public SettingVisibilityRegistrationValue(Func<bool> isVisible)
            {
                IsVisible = isVisible;
            }

            internal void FireUpdateVisibility() => OnUpdateVisibility?.Invoke();
        }
        
        /// <summary>
        /// Actions added to this even will be called during UI Expansion Kit init, after VrcUiManager has been created
        /// <exception cref="InvalidOperationException">Thrown if an action is attempted to be added after UI Expansion Kit is initialized (and VrcUiManager is already created)</exception>
        /// </summary>
        public static event Action OnUiManagerInit
        {
            add
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (onUiManagerInitDelegateList == null)
                    throw new InvalidOperationException("UI manager init has already happened, your delegate will not be called");
                onUiManagerInitDelegateList.Add(value);
            }
            remove
            {
                if (onUiManagerInitDelegateList != null) onUiManagerInitDelegateList.Remove(value);
            }
        }
        
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
                MelonLogger.Error($"Custom UI for category {categoryName} is already registered");
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

        /// <summary>
        /// Returns the interface that can be used to add buttons to settings categories.
        /// If they category was registered as custom via <see cref="RegisterCustomSettingsCategory"/>, changes to this menu will have no effect.
        /// </summary>
        /// <param name="categoryName">The category to return the menu for</param>
        public static ICustomLayoutedMenu GetSettingsCategory(string categoryName)
        {
            if (SettingPageExtensions.TryGetValue(categoryName, out var result)) return result;

            return SettingPageExtensions[categoryName] = new CustomLayoutedPageImpl(null);
        }

        internal class ButtonRegistration
        {
            public GameObject Prefab;
            
            public Action<GameObject> InstanceConsumer;
            
            public string Text;
            
            public Action Action;
            
            public Action<bool> ToggleAction;
            public Func<bool> InitialState;

            public override string ToString()
            {
                return $"{Text} Prefab={Prefab?.name} IsButton={Action != null} IsToggle={ToggleAction != null}";
            }
        }

        /// <summary>
        /// Registers a coroutine that will be waited for before creating menu decorations.
        /// This can be used to delay decoration creation if your mod has async operations required to load custom UI prefabs.
        /// </summary>
        public static void RegisterWaitConditionBeforeDecorating(IEnumerator coroutine)
        {
            if (CanAddWaitCoroutines)
                ExtraWaitCoroutines.Add(coroutine);
            else
                throw new InvalidOperationException("UIX init is already running or is complete, it's too late to register wait conditions");
        }

        /// <summary>
        /// Registers a specific string-valued MelonPref as a enum value.
        /// In mod settings menu, this setting will be represented by a dropdown with specified possible values.
        /// The list of possible values will be read each time the settings UI is shown
        /// </summary>
        /// <param name="categoryName">MelonPrefs settings category</param>
        /// <param name="settingName">MelonPrefs setting name</param>
        /// <param name="possibleValues">A list of possible values</param>
        public static void RegisterSettingAsStringEnum(string categoryName, string settingName, IList<(string SettingsValue, string DisplayName)> possibleValues)
        {
            EnumSettings[(categoryName, settingName)] = possibleValues;
        }

        /// <summary>
        /// Creates a custom quick menu page.
        /// When shown, the page will be positioned above quick menu, overlapping the main 4x4 grid.
        /// </summary>
        /// <param name="requestedLayout">The layout of the page. If null, a custom layout is assumed - your mod code will need to assign sizes and positions to buttons manually</param>
        public static ICustomShowableLayoutedMenu CreateCustomQuickMenuPage(LayoutDescription? requestedLayout)
        {
            return new CustomQuickMenuPageImpl(requestedLayout);
        }
        
        /// <summary>
        /// Creates a custom quick menu page.
        /// When shown, the page will be positioned over the camera expando.
        /// </summary>
        /// <param name="requestedLayout">The layout of the page. If null, a custom layout is assumed - your mod code will need to assign sizes and positions to buttons manually</param>
        public static ICustomShowableLayoutedMenu CreateCustomCameraExpandoPage(LayoutDescription? requestedLayout)
        {
            return new CustomCameraPageImpl(requestedLayout);
        }
        
        
        /// <summary>
        /// Creates a custom quick menu expando overlay page.
        /// When shown, the page will be positioned over the quick menu expando.
        /// This overlay is not affected by which quick menu page is shown, or by current page's expando visibility.
        /// </summary>
        /// <param name="requestedLayout">The layout of the page. If null, a custom layout is assumed - your mod code will need to assign sizes and positions to buttons manually</param>
        public static ICustomShowableLayoutedMenu CreateCustomQmExpandoPage(LayoutDescription? requestedLayout)
        {
            return new CustomExpandoOverlayImpl(requestedLayout);
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

        /// <summary>
        /// Returns the object containing other objects loaded from UI Expansion Kit resources assetbundle
        /// Return type of this method and layout of that class are subject to change - only use if strictly necessary
        /// </summary>
        public static PreloadedBundleContents GetUiExpansionKitBundleContents()
        {
            return UiExpansionKitMod.Instance.StuffBundle;
        }

        /// <summary>
        /// Registers a visibility callback for a given settings entry.
        /// </summary>
        /// <returns>A delegate that can be called to update visibility of settings entry</returns>
        public static Action RegisterSettingsVisibilityCallback(string category, string setting, Func<bool> isVisible)
        {
            var value = new SettingVisibilityRegistrationValue(isVisible);
            SettingsVisibilities[(category, setting)] = value;
            return value.FireUpdateVisibility;
        }
    }
}