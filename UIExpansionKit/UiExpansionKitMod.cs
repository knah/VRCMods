using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UIExpansionKit;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRCSDK2;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(UiExpansionKitMod), "UI Expansion Kit", "0.2.6", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace UIExpansionKit
{
    internal class UiExpansionKitMod : CustomizedMelonMod
    {
        internal static UiExpansionKitMod Instance;
        
        private PreloadedBundleContents myStuffBundle;

        private GameObject myModSettingsExpando;
        private GameObject mySettingsPage;
        private Transform myModSettingsExpandoTransform;

        private GameObject myInputPopup;
        private GameObject myInputKeypadPopup;
        
        private static readonly List<(ExpandedMenu, string, bool isFullMenu)> GameObjectToCategoryList = new List<(ExpandedMenu, string, bool)>
        {
            (ExpandedMenu.AvatarMenu, "UserInterface/MenuContent/Screens/Avatar", true),
            (ExpandedMenu.SafetyMenu, "UserInterface/MenuContent/Screens/Settings_Safety", true),
            (ExpandedMenu.SettingsMenu, "UserInterface/MenuContent/Screens/Settings", true),
            (ExpandedMenu.WorldMenu, "UserInterface/MenuContent/Screens/Worlds", true),
            (ExpandedMenu.WorldDetailsMenu, "UserInterface/MenuContent/Screens/WorldInfo", true),
            (ExpandedMenu.UserDetailsMenu, "UserInterface/MenuContent/Screens/UserInfo", true),
            (ExpandedMenu.SocialMenu, "UserInterface/MenuContent/Screens/Social", true),
            
            (ExpandedMenu.QuickMenu, "UserInterface/QuickMenu/ShortcutMenu", false),
            (ExpandedMenu.UserQuickMenu, "UserInterface/QuickMenu/UserInteractMenu", false),
            (ExpandedMenu.EmojiQuickMenu, "UserInterface/QuickMenu/EmojiMenu", false),
            (ExpandedMenu.EmoteQuickMenu, "UserInterface/QuickMenu/EmoteMenu", false),
            (ExpandedMenu.CameraQuickMenu, "UserInterface/QuickMenu/CameraMenu", false),
            (ExpandedMenu.ModerationQuickMenu, "UserInterface/QuickMenu/ModerationMenu", false),
            (ExpandedMenu.UiElementsQuickMenu, "UserInterface/QuickMenu/UIElementsMenu", false),
            (ExpandedMenu.AvatarStatsQuickMenu, "UserInterface/QuickMenu/AvatarStatsMenu", false),
            (ExpandedMenu.InvitesTab, "UserInterface/QuickMenu/QuickModeMenus/QuickModeNotificationsMenu", false),
        };
        
        private readonly Dictionary<ExpandedMenu, GameObject> myMenuRoots = new Dictionary<ExpandedMenu, GameObject>();
        private readonly Dictionary<ExpandedMenu, GameObject> myVisibilitySources = new Dictionary<ExpandedMenu, GameObject>();
        private readonly Dictionary<ExpandedMenu, bool> myHasContents = new Dictionary<ExpandedMenu, bool>();

        public PreloadedBundleContents StuffBundle => myStuffBundle;
        
        internal static bool AreSettingsDirty = false;

        public override void OnApplicationStart()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();
            ClassInjector.RegisterTypeInIl2Cpp<DestroyListener>();
            
            ExpansionKitSettings.RegisterSettings();
            ExpansionKitSettings.PinsEntry.OnValueChangedUntyped += UpdateQuickMenuPins;
            MelonCoroutines.Start(InitThings());
        }

        public void UpdateQuickMenuPins()
        {
            if (myMenuRoots.TryGetValue(ExpandedMenu.QuickMenu, out var menuRoot))
            {
                FillQuickMenuExpando(menuRoot, ExpandedMenu.QuickMenu);
                UpdateCategoryVisibility(ExpandedMenu.QuickMenu);
            }
        }

        private void UpdateCategoryVisibility(ExpandedMenu category)
        {
            if (myMenuRoots.TryGetValue(category, out var menuRoot) &&
                myHasContents.TryGetValue(category, out var hasContents) &&
                myVisibilitySources.TryGetValue(category, out var visibilitySource))
            {
                menuRoot.SetActive(hasContents && visibilitySource.activeInHierarchy);
            }
        }

        private IEnumerator InitThings()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null)
                yield return null;

            while (QuickMenu.prop_QuickMenu_0 == null)
                yield return null;
            
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UIExpansionKit.modui.assetbundle");
                using var memStream = new MemoryStream((int) stream.Length);
                stream.CopyTo(memStream);
                var assetBundle = AssetBundle.LoadFromMemory_Internal(memStream.ToArray(), 0);
                assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                
                myStuffBundle = new PreloadedBundleContents(assetBundle);
            }
            
            // attach it to QuickMenu. VRChat changes render queue on QM contents on world load that makes it render properly
            myStuffBundle.StoredThingsParent.transform.SetParent(QuickMenu.prop_QuickMenu_0.transform);
            
            myInputPopup = GameObject.Find("UserInterface/MenuContent/Popups/InputPopup");
            myInputKeypadPopup = GameObject.Find("UserInterface/MenuContent/Popups/InputKeypadPopup");

            var listener = myInputPopup.GetOrAddComponent<EnableDisableListener>();
            listener.OnEnabled += UpdateModSettingsVisibility;
            listener.OnDisabled += UpdateModSettingsVisibility;
            listener = myInputKeypadPopup.GetOrAddComponent<EnableDisableListener>();
            listener.OnEnabled += UpdateModSettingsVisibility;
            listener.OnDisabled += UpdateModSettingsVisibility;

            var delegatesToInvoke = ExpansionKitApi.onUiManagerInitDelegateList;
            ExpansionKitApi.onUiManagerInitDelegateList = null;
            foreach (var action in delegatesToInvoke)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error while invoking UI-manager-init delegate {action.GetType().FullName}: {ex}");
                }
            }

            foreach (var coroutine in ExpansionKitApi.ExtraWaitCoroutines)
            {
                while (true)
                {
                    try
                    {
                        if (!coroutine.MoveNext()) break;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"Error while waiting for init of coroutine with type {coroutine.GetType().FullName}: {ex}");
                        break;
                    }
                    yield return coroutine.Current;
                }
            }

            GameObject.Find("UserInterface/QuickMenu/QuickMenu_NewElements/_Background")
                .AddComponent<EnableDisableListener>().OnDisabled += BuiltinUiUtils.InvokeQuickMenuClosed;
            
            GameObject.Find("UserInterface/MenuContent/Backdrop/Backdrop")
                .AddComponent<EnableDisableListener>().OnDisabled += BuiltinUiUtils.InvokeFullMenuClosed;

            DecorateFullMenu();
            DecorateMenuPages();
        }

        private void DecorateMenuPages()
        {
            MelonLogger.Msg("Decorating menus");
            
            var quickMenuExpandoPrefab = myStuffBundle.QuickMenuExpando;
            var quickMenuRoot = QuickMenu.prop_QuickMenu_0.gameObject;
            
            var fullMenuExpandoPrefab = myStuffBundle.BigMenuExpando;
            var fullMenuRoot = VRCUiManager.prop_VRCUiManager_0.field_Public_GameObject_0;
            
            foreach (var valueTuple in GameObjectToCategoryList)
            {
                var categoryEnum = valueTuple.Item1;
                var gameObjectPath = valueTuple.Item2;
                var isBigMenu = valueTuple.Item3;

                var gameObject = GameObject.Find(gameObjectPath);
                if (gameObject == null)
                {
                    MelonLogger.Error($"GameObject at path {gameObjectPath} for category {categoryEnum} was not found, not decorating");
                    continue;
                }
                
                myVisibilitySources[categoryEnum] = gameObject;

                if (isBigMenu)
                {
                    var expando = Object.Instantiate(fullMenuExpandoPrefab, fullMenuRoot.transform, false);
                    myMenuRoots[categoryEnum] = expando;
                    var expandoTransform = expando.transform;
                    expandoTransform.localScale = Vector3.one * 2;
                    expandoTransform.localPosition = new Vector3(-775, -435, -15);
                    expando.AddComponent<VRC_UiShape>();
                    expando.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                    {
                        var compo = expando.GetComponent<VerticalLayoutGroup>();
                        var willBeRight = compo.childAlignment == TextAnchor.LowerLeft;
                        compo.childAlignment = willBeRight
                            ? TextAnchor.LowerRight
                            : TextAnchor.LowerLeft;

                        if (categoryEnum == ExpandedMenu.AvatarMenu)
                            gameObject.transform.Find("AvatarPreviewBase").gameObject.SetActive(!willBeRight);
                    }));
                    
                    var listener = gameObject.GetOrAddComponent<EnableDisableListener>();
                    listener.OnEnabled += () =>
                    {
                        expando.SetActive(myHasContents[categoryEnum]);
                        BuiltinUiUtils.InvokeMenuOpened(categoryEnum);
                    };
                    listener.OnDisabled += () => expando.SetActive(false);

                    FillBigMenuExpando(expando, categoryEnum);

                    SetLayerRecursively(expando, gameObject.layer);
                }
                else
                {
                    var expando = Object.Instantiate(quickMenuExpandoPrefab, quickMenuRoot.transform, false);
                    myMenuRoots[categoryEnum] = expando;

                    var transform = expando.transform;
                    transform.localScale = Vector3.one * 4.2f; // looks like the original menu already has scale of 0.001
                    transform.RotateAround(transform.position, transform.right, 30);
                    transform.Cast<RectTransform>().localPosition = new Vector3(55, -1000, 5);

                    var toggleButton = transform.Find("QuickMenuExpandoToggle");
                    var content = transform.Find("Content");
                    toggleButton.gameObject.AddComponent<VRC_UiShape>();
                    content.gameObject.AddComponent<VRC_UiShape>();

                    if (ExpansionKitSettings.IsQmExpandoStartsCollapsed())
                        toggleButton.GetComponent<Toggle>().isOn = false;
                    
                    var listener = gameObject.GetOrAddComponent<EnableDisableListener>();
                    listener.OnEnabled += () =>
                    {
                        expando.SetActive(myHasContents[categoryEnum]);
                        BuiltinUiUtils.InvokeMenuOpened(categoryEnum);
                    };
                    listener.OnDisabled += () => expando.SetActive(false);
                    
                    FillQuickMenuExpando(expando, categoryEnum);

                    expando.GetOrAddComponent<EnableDisableListener>().OnEnabled += () =>
                    {
                        MelonCoroutines.Start(ResizeExpandoAfterDelay(expando));
                    };
                    
                    SetLayerRecursively(expando, quickMenuRoot.layer);
                }
                
                UpdateCategoryVisibility(valueTuple.Item1);
            }
        }

        public override void OnUpdate()
        {
            TaskUtilities.ourMainThreadQueue.Flush();
        }

        public override void OnGUI()
        {
            TaskUtilities.ourFrameEndQueue.Flush();
        }

        private static IEnumerator ResizeExpandoAfterDelay(GameObject expando)
        {
            yield return null;
            DoResizeExpando(expando);
        }

        private static void DoResizeExpando(GameObject expando)
        {
            var totalButtons = 0;
            foreach (var o in expando.transform.Find("Content/Scroll View/Viewport/Content"))
            {
                if (o.Cast<Transform>().gameObject.activeSelf)
                    totalButtons++;
            }
            
            var targetRows = ExpansionKitSettings.ClampQuickMenuExpandoRowCount((totalButtons + 3) / 4);
            var expandoRectTransform = expando.transform.Cast<RectTransform>();
            var oldPosition = expandoRectTransform.anchoredPosition;
            expandoRectTransform.sizeDelta = new Vector2(expandoRectTransform.sizeDelta.x, 100 * targetRows + 5);
            expandoRectTransform.anchoredPosition = oldPosition;
            expando.transform.Find("Content").GetComponent<VRC_UiShape>().Awake(); // adjust the box collider for raycasts
        }

        private void FillBigMenuExpando(GameObject expando, ExpandedMenu categoryEnum)
        {
            var expandoRoot = expando.transform.Find("Content").Cast<RectTransform>();

            myHasContents[categoryEnum] = false;
            
            expandoRoot.DestroyChildren();

            if (ExpansionKitApi.ExpandedMenus.TryGetValue(categoryEnum, out var registrations))
            {
                myHasContents[categoryEnum] = true;
                registrations.PopulateButtons(expandoRoot, false, false);
            }
        }

        private void UpdateModSettingsVisibility()
        {
            var wasActive = myModSettingsExpando.activeSelf;
            var newActive = mySettingsPage.activeInHierarchy && !(myInputPopup.activeSelf || myInputKeypadPopup.activeSelf);
            myModSettingsExpando.SetActive(newActive);

            if (wasActive != newActive && newActive == false && AreSettingsDirty)
            {
                MelonPreferences.Save();
                AreSettingsDirty = false;
            }
        }

        private void DecorateFullMenu()
        {
            var fullMenuRoot = VRCUiManager.prop_VRCUiManager_0.field_Public_GameObject_0;

            var settingsExpandoPrefab = myStuffBundle.SettingsMenuExpando;
            myModSettingsExpando = Object.Instantiate(settingsExpandoPrefab, fullMenuRoot.transform, false);
            myModSettingsExpandoTransform = myModSettingsExpando.transform;
            myModSettingsExpandoTransform.localScale = Vector3.one * 1.52f;
            myModSettingsExpandoTransform.localPosition = new Vector3(-755, -550, -10);
            myModSettingsExpando.AddComponent<VRC_UiShape>();
            myModSettingsExpando.SetActive(false);

            ModSettingsHandler.Initialize(myStuffBundle);
            var settingsContentRoot = myModSettingsExpando.transform.Find("Content/Scroll View/Viewport/Content").Cast<RectTransform>();
            MelonCoroutines.Start(ModSettingsHandler.PopulateSettingsPanel(settingsContentRoot));

            mySettingsPage = fullMenuRoot.transform.Find("Screens/Settings").gameObject;
            var settingsMenuListener = mySettingsPage.GetOrAddComponent<EnableDisableListener>();
            settingsMenuListener.OnEnabled += UpdateModSettingsVisibility;
            settingsMenuListener.OnDisabled += UpdateModSettingsVisibility;

            Object.Destroy(myModSettingsExpandoTransform.Find("Content/ApplyButton").gameObject);
            Object.Destroy(myModSettingsExpandoTransform.Find("Content/RefreshButton").gameObject);
            
            SetLayerRecursively(myModSettingsExpando, mySettingsPage.gameObject.layer);
        }

        internal static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (var o in obj.transform) 
                SetLayerRecursively(o.Cast<Transform>().gameObject, layer);
        }

        private void FillQuickMenuExpando(GameObject expando, ExpandedMenu expandedMenu)
        {
            var expandoRoot = expando.transform.Find("Content/Scroll View/Viewport/Content").Cast<RectTransform>();
            
            expandoRoot.DestroyChildren();

            var toggleButtonPrefab = myStuffBundle.QuickMenuToggle;

            myHasContents[expandedMenu] = false;

            if (ExpansionKitApi.ExpandedMenus.TryGetValue(expandedMenu, out var registrations))
            {
                registrations.PopulateButtons(expandoRoot, true, false);

                myHasContents[expandedMenu] = true;
            }

            if (expandedMenu == ExpandedMenu.QuickMenu)
            {
                foreach (var (category, prefId) in ExpansionKitSettings.ListPinnedPrefs())
                {
                    var entry = MelonPreferences.GetCategory(category)?.GetEntry(prefId);
                    if (entry is not MelonPreferences_Entry<bool> boolEntry) continue;

                    var toggleButton = Object.Instantiate(toggleButtonPrefab, expandoRoot, false);
                    toggleButton.GetComponentInChildren<Text>().text = entry.DisplayName ?? prefId;
                    var toggle = toggleButton.GetComponent<Toggle>();
                    toggle.isOn = boolEntry.Value;
                    toggle.onValueChanged.AddListener(new Action<bool>(isOn =>
                    {
                        if (boolEntry.Value != isOn)
                        {
                            boolEntry.Value = isOn;
                            MelonPreferences.Save();
                        }
                    }));
                    Action<bool, bool> handler = (_, newValue) =>
                    {
                        toggle.isOn = newValue;
                    };
                    boolEntry.OnValueChanged += handler;
                    toggleButton.GetOrAddComponent<DestroyListener>().OnDestroyed += () =>
                    {
                        boolEntry.OnValueChanged -= handler;
                    };
                    
                    myHasContents[expandedMenu] = true;
                }
            }
            
            DoResizeExpando(expando);
        }
    }
}