using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
using UIExpansionKit;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UnityEngine.XR;
using VRC.UI.Core.Styles;
using VRC.UserCamera;
using VRCSDK2;
using Object = UnityEngine.Object;
using QuickMenuNew = VRC.UI.Elements.QuickMenu;

[assembly:MelonInfo(typeof(UiExpansionKitMod), "UI Expansion Kit", "1.0.1", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace UIExpansionKit
{
    internal partial class UiExpansionKitMod : MelonMod
    {
        internal static UiExpansionKitMod Instance;
        
        private PreloadedBundleContents myStuffBundle;

        private GameObject myModSettingsExpando;
        private GameObject mySettingsPage;
        private Transform myModSettingsExpandoTransform;

        private GameObject myInputPopup;
        private GameObject myInputKeypadPopup;
        internal Transform myCameraExpandoRoot;
        internal Transform myQmExpandosRoot;
        
        private static readonly List<(ExpandedMenu, string, bool isFullMenu)> GameObjectToCategoryList = new List<(ExpandedMenu, string, bool)>
        {
            (ExpandedMenu.AvatarMenu, "UserInterface/MenuContent/Screens/Avatar", true),
            (ExpandedMenu.SafetyMenu, "UserInterface/MenuContent/Screens/Settings_Safety", true),
            (ExpandedMenu.SettingsMenu, "UserInterface/MenuContent/Screens/Settings", true),
            (ExpandedMenu.WorldMenu, "UserInterface/MenuContent/Screens/Worlds", true),
            (ExpandedMenu.WorldDetailsMenu, "UserInterface/MenuContent/Screens/WorldInfo", true),
            (ExpandedMenu.UserDetailsMenu, "UserInterface/MenuContent/Screens/UserInfo", true),
            (ExpandedMenu.SocialMenu, "UserInterface/MenuContent/Screens/Social", true),
            
            (ExpandedMenu.QuickMenu,            "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Dashboard", false),
            (ExpandedMenu.UserQuickMenu,        "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_SelectedUser_Local", false),
            (ExpandedMenu.UserQuickMenuRemote,        "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_SelectedUser_Remote", false),
            (ExpandedMenu.EmojiQuickMenu,       "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_QM_Emojis", false),
            (ExpandedMenu.QuickMenuHere,       "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Here", false),
            (ExpandedMenu.CameraQuickMenu,      "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Camera", false),
            (ExpandedMenu.QuickMenuAudioSettings,  "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_AudioSettings", false),
            (ExpandedMenu.UiElementsQuickMenu,  "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Settings", false),
            (ExpandedMenu.InvitesTab,           "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_Notifications", false),
            (ExpandedMenu.AvatarStatsQuickMenu,           "UserInterface/Canvas_QuickMenu(Clone)/Container/Window/QMParent/Menu_QM_AvatarDetails", false),
        };
        
        private readonly Dictionary<ExpandedMenu, GameObject> myMenuRoots = new();
        private readonly Dictionary<ExpandedMenu, GameObject> myVisibilitySources = new();
        private readonly Dictionary<ExpandedMenu, bool> myHasContents = new();

        public PreloadedBundleContents StuffBundle => myStuffBundle;
        
        internal static bool AreSettingsDirty = false;

        private static bool IsInDesktop;
        
        internal static QuickMenuNew? GetQuickMenu() => UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/Canvas_QuickMenu(Clone)")?.GetComponent<QuickMenuNew>();

        public override void OnApplicationStart()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();
            ClassInjector.RegisterTypeInIl2Cpp<DestroyListener>();
            ClassInjector.RegisterTypeInIl2Cpp<StyleElementWrapper>();
            ClassInjector.RegisterTypeInIl2Cpp<StyleEngineUpdateDriver>();

            StylingHelper.Init();

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
            while (GetUiManager() == null)
                yield return null;

            while (GetQuickMenu() == null)
                yield return null;

            if (!CheckWasSuccessful) yield break;
            
            IsInDesktop = !XRDevice.isPresent || Environment.CommandLine.Contains("--no-vr");
            
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UIExpansionKit.Resources.modui.assetbundle");
                using var memStream = new MemoryStream((int) stream.Length);
                stream.CopyTo(memStream);
                var assetBundle = AssetBundle.LoadFromMemory_Internal(memStream.ToArray(), 0);
                assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                
                myStuffBundle = new PreloadedBundleContents(assetBundle);
            }
            
            // attach it to QuickMenu. VRChat changes render queue on QM contents on world load that makes it render properly
            myStuffBundle.StoredThingsParent.transform.SetParent(GetQuickMenu().transform);

            StylingHelper.StyleEngine = GetQuickMenu().GetComponent<StyleEngine>();

            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UIExpansionKit.Resources.uix-style-main.vrcss");
                using var memStream = new MemoryStream((int) stream.Length);
                stream.CopyTo(memStream);
                var newStyle = Encoding.UTF8.GetString(memStream.ToArray());
                var resourcesList = StylingHelper.StyleEngine.field_Public_StyleResource_0.resources;
                for (var i = 0; i < resourcesList.Count; i++)
                {
                    var resource = resourcesList[i];
                    if (resource.address != "style-sheet") continue;

                    resource.address = "style-sheet-uix-original";
                    resourcesList[i] = resource;
                    break;
                }
                
                resourcesList.Add(new StyleResource.Resource { address = "style-sheet", obj = new TextAsset(TextAsset.CreateOptions.CreateNativeObject, newStyle) });
            }
            

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

            var waitConditions = ExpansionKitApi.ExtraWaitCoroutines.ToList();
            ExpansionKitApi.ExtraWaitCoroutines.Clear();
            ExpansionKitApi.CanAddWaitCoroutines = false;
            foreach (var coroutine in waitConditions)
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

            myInputPopup = GameObject.Find("UserInterface/MenuContent/Popups/InputPopup");
            myInputKeypadPopup = GameObject.Find("UserInterface/MenuContent/Popups/InputKeypadPopup");
            
            // Wait an extra frame to ve very sure that all other mods had the chance to register buttons in their wait-for-ui-manager coroutine
            yield return null;

            var listener = myInputPopup.GetOrAddComponent<EnableDisableListener>();
            listener.OnEnabled += UpdateModSettingsVisibility;
            listener.OnDisabled += UpdateModSettingsVisibility;
            listener = myInputKeypadPopup.GetOrAddComponent<EnableDisableListener>();
            listener.OnEnabled += UpdateModSettingsVisibility;
            listener.OnDisabled += UpdateModSettingsVisibility;

            UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/Canvas_QuickMenu(Clone)/Container")
                .AddComponent<EnableDisableListener>().OnDisabled += BuiltinUiUtils.InvokeQuickMenuClosed;
            
            var mainMenuBackground = GameObject.Find("UserInterface/MenuContent/Backdrop/Backdrop");
            mainMenuBackground.AddComponent<EnableDisableListener>().OnDisabled += BuiltinUiUtils.InvokeFullMenuClosed;
            mainMenuBackground.AddComponent<StyleEngineUpdateDriver>().StyleEngine = StylingHelper.StyleEngine;

            DecorateFullMenu();
            CheckA();
            DecorateMenuPages();
            DecorateCamera();
        }

        private void DecorateMenuPages()
        {
            MelonLogger.Msg("Decorating menus");
            
            var quickMenuExpandoPrefab = myStuffBundle.QuickMenuExpando;
            var quickMenuRoot = GetQuickMenu().transform.Find("Container").gameObject;
            
            var fullMenuExpandoPrefab = myStuffBundle.BigMenuExpando;
            var fullMenuRoot = GetUiManager().field_Public_GameObject_0;

            var qmExpandosRootGo = new GameObject("UIX QM Expandos Root", new []{Il2CppType.Of<RectTransform>()});
            myQmExpandosRoot = qmExpandosRootGo.transform;

            var qmExpandosXform = myQmExpandosRoot.Cast<RectTransform>();
            qmExpandosXform.SetParent(quickMenuRoot.transform, false);
            qmExpandosXform.localScale = Vector3.one * 3f; // looks like the original menu already has scale of 0.001
            if (!IsInDesktop)
                qmExpandosXform.RotateAround(qmExpandosXform.position, qmExpandosXform.right, 30);
            qmExpandosXform.Cast<RectTransform>().localPosition = new Vector3(55, IsInDesktop ? 500 : -750, -5);

            foreach (var valueTuple in GameObjectToCategoryList)
            {
                var categoryEnum = valueTuple.Item1;
                var gameObjectPath = valueTuple.Item2;
                var isBigMenu = valueTuple.Item3;

                var gameObject = UnityUtils.FindInactiveObjectInActiveRoot(gameObjectPath);
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

                    // todo: reparent to expandos root?
                    var transform = expando.transform;
                    transform.localScale = Vector3.one * 3f; // the original menu already has scale of 0.0005
                    if (!IsInDesktop) 
                        transform.RotateAround(transform.position, transform.right, 30);

                    transform.Cast<RectTransform>().localPosition = new Vector3(55, -750, -5);

                    var toggleButton = transform.Find("QuickMenuExpandoToggle");
                    var flipButton = transform.Find("QuickMenuFlipUp");
                    var content = transform.Find("Content");
                    toggleButton.gameObject.AddComponent<VRC_UiShape>();
                    flipButton.gameObject.AddComponent<VRC_UiShape>();
                    content.gameObject.AddComponent<VRC_UiShape>();
                    var toggle = toggleButton.GetComponent<Toggle>();

                    if (IsInDesktop)
                    {
                        var flipToggle = flipButton.GetComponent<Toggle>();
                        var flipIcon = flipButton.Find("Image");
                        
                        flipToggle.isOn = false;
                        flipToggle.onValueChanged.AddListener(new Action<bool>(isUp =>
                        {
                            flipIcon.localEulerAngles = isUp ? new Vector3(0, 0, 180) : Vector3.zero;
                            transform.Cast<RectTransform>().localPosition = new Vector3(55, isUp ? 500 : -750, -5);
                        }));
                    }
                    else
                    {
                        flipButton.gameObject.SetActive(false);
                    }

                    if (ExpansionKitSettings.IsQmExpandoStartsCollapsed()) 
                        toggle.isOn = false;
                    
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
                        MelonCoroutines.Start(ResizeExpandoAfterDelay(expando, toggle.isOn));
                    };
                    
                    SetLayerRecursively(expando, quickMenuRoot.layer);
                }
                
                UpdateCategoryVisibility(valueTuple.Item1);
            }
            
            myQmExpandosRoot.SetAsLastSibling();
        }

        private void DecorateCamera()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null)
            {
                MelonLogger.Warning("Camera controller not found, not decorating the camera");
                return;
            }
            
            var cameraTransform = cameraController.transform.Find("ViewFinder");
            var controlsTransform = cameraTransform.Find("PhotoControls");
            var dummyTransform = new GameObject("UixCameraDummy");
            dummyTransform.transform.SetParent(cameraTransform, false);
            var constraint = dummyTransform.AddComponent<ParentConstraint>();
            constraint.AddSource(new ConstraintSource() { sourceTransform = controlsTransform, weight = 1 });
            constraint.constraintActive = true;
            StylingHelper.AddStyleElement(dummyTransform, "");
            dummyTransform.AddComponent<StyleEngineUpdateDriver>().StyleEngine = StylingHelper.StyleEngine;
            
            var expando = Object.Instantiate(myStuffBundle.QuickMenuExpando, dummyTransform.transform, false);
            myMenuRoots[ExpandedMenu.Camera] = expando;

            var transform = expando.transform;
            myCameraExpandoRoot = transform;
            transform.localScale = Vector3.one * 0.0004f;
            transform.localPosition = new Vector3(0, 0.005f, -0.135f);
            transform.localRotation = Quaternion.Euler(90, 180, 0);

            var toggleButton = transform.Find("QuickMenuExpandoToggle");
            var content = transform.Find("Content").Cast<RectTransform>();
            var flipButton = transform.Find("QuickMenuFlipUp");
            content.pivot = new Vector2(0.5f, 0f);
            Object.Destroy(flipButton.gameObject);
            toggleButton.gameObject.AddUiShapeWithTriggerCollider();
            toggleButton.GetComponent<StyleElementWrapper>().AdditionalClass = "UixCameraExpandoToggleButton";
            content.gameObject.AddUiShapeWithTriggerCollider();
            toggleButton.localPosition += Vector3.left * 60 + new Vector3(0, -25, 0);
            toggleButton.localScale = Vector3.one * 3;
            var toggleComponent = toggleButton.GetComponent<Toggle>();

            if (ExpansionKitSettings.IsCameraExpandoStartsCollapsed()) 
                toggleComponent.isOn = false;

            var listener = cameraTransform.gameObject.GetOrAddComponent<EnableDisableListener>();
            listener.OnEnabled += () =>
            {
                expando.SetActive(myHasContents[ExpandedMenu.Camera]);
                BuiltinUiUtils.InvokeMenuOpened(ExpandedMenu.Camera);
            };
            listener.OnDisabled += () => expando.SetActive(false);

            FillQuickMenuExpando(expando, ExpandedMenu.Camera);

            expando.GetOrAddComponent<EnableDisableListener>().OnEnabled += () =>
            {
                MelonCoroutines.Start(ResizeExpandoAfterDelay(expando, toggleComponent.isOn, () => content.localPosition = new Vector3(0, -105, 0)));
            };

            SetLayerRecursively(expando, cameraTransform.Find("PhotoControls").gameObject.layer);
        }

        public override void OnUpdate()
        {
            TaskUtilities.ourMainThreadQueue.Flush();
        }

        public override void OnGUI()
        {
            TaskUtilities.ourFrameEndQueue.Flush();
        }

        private static IEnumerator ResizeExpandoAfterDelay(GameObject expando, bool contentsCanBeVisible, Action afterResize = null)
        {
            yield return null;
            DoResizeExpando(expando, contentsCanBeVisible);
            if (afterResize == null) yield break;
            
            yield return null;
            afterResize();
        }

        private static void DoResizeExpando(GameObject expando, bool contentsCanBeVisible)
        {
            var totalButtons = 0;
            foreach (var o in expando.transform.Find("Content/Scroll View/Viewport/Content"))
            {
                if (o.Cast<Transform>().gameObject.activeSelf)
                    totalButtons++;
            }
            
            var content = expando.transform.Find("Content");
            var targetRows = ExpansionKitSettings.ClampQuickMenuExpandoRowCount((totalButtons + 3) / 4);
            var expandoRectTransform = expando.transform.Cast<RectTransform>();
            var oldPosition = expandoRectTransform.anchoredPosition;
            expandoRectTransform.sizeDelta = new Vector2(expandoRectTransform.sizeDelta.x, 100 * targetRows + 5);
            expandoRectTransform.anchoredPosition = oldPosition;
            content.GetComponent<VRC_UiShape>().OnRectTransformDimensionsChange(); // adjust the box collider for raycasts
            content.gameObject.SetActive(totalButtons != 0 && contentsCanBeVisible);
            expando.transform.Find("QuickMenuExpandoToggle").gameObject.SetActive(totalButtons != 0);
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
            var fullMenuRoot = GetUiManager().field_Public_GameObject_0;
            CheckC();

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
                    if (entry == null) continue;
                    
                    if (PinnedPrefUtil.CreatePinnedPrefButton(entry, expandoRoot, myStuffBundle))
                        myHasContents[expandedMenu] = true;
                }
            }
            
            DoResizeExpando(expando, expando.transform.Find("QuickMenuExpandoToggle").GetComponent<Toggle>().isOn);
        }

        private static void SetActiveAfterDelay(GameObject obj, bool active)
        {
            MelonCoroutines.Start(SetActiveAfterDelayImpl(obj, active));
        }

        private static IEnumerator SetActiveAfterDelayImpl(GameObject gameObject, bool active)
        {
            yield return null;
            yield return null;
            gameObject.SetActive(active);
        }
    }
}