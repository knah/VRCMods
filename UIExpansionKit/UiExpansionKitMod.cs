using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using UIExpansionKit;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRCSDK2;
using Object = UnityEngine.Object;

[assembly:MelonModInfo(typeof(UiExpansionKitMod), "UI Expansion Kit", "0.1.1", "knah")]
[assembly:MelonModGame("VRChat", "VRChat")]

namespace UIExpansionKit
{
    public class UiExpansionKitMod : MelonMod
    {
        private PreloadedBundleContents myStuffBundle;

        private GameObject myModSettingsExpando;
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
        };
        
        private readonly Dictionary<ExpandedMenu, GameObject> myMenuRoots = new Dictionary<ExpandedMenu, GameObject>();
        private readonly List<(GameObject from, GameObject to)> myVisibilityTransfers = new List<(GameObject from, GameObject to)>();
        private readonly Dictionary<GameObject, bool> myHasContents = new Dictionary<GameObject, bool>();
        
        public override void OnApplicationStart()
        {
            ExpansionKitSettings.RegisterSettings();
            MelonCoroutines.Start(InitThings());
        }

        public override void OnUpdate()
        {
            // todo: replace with component when custom components are a thing
            foreach (var visibilityTransfer in myVisibilityTransfers)
                visibilityTransfer.to.SetActive(myHasContents.TryGetValue(visibilityTransfer.to, out var hasContents) && hasContents && visibilityTransfer.from.activeSelf);

            if (myInputPopup != null && myModSettingsExpando != null)
                if (myInputPopup.activeSelf || myInputKeypadPopup.activeSelf)
                    myModSettingsExpando.SetActive(false);
        }

        public override void OnModSettingsApplied()
        {
            if (myMenuRoots.TryGetValue(ExpandedMenu.QuickMenu, out var menuRoot))
                FillQuickMenuExpando(menuRoot, ExpandedMenu.QuickMenu);
        }

        private IEnumerator InitThings()
        {
            while (VRCUiManager.field_Protected_Static_VRCUiManager_0 == null)
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
                        MelonModLogger.LogError(
                            $"Error while waiting for init of coroutine with type {coroutine.GetType().FullName}: {ex.ToString()}");
                    }
                    yield return coroutine.Current;
                }
            }

            DecorateFullMenu();
            DecorateMenuPages();
        }

        private void DecorateMenuPages()
        {
            MelonModLogger.Log($"Decorating menus");
            
            var quickMenuExpandoPrefab = myStuffBundle.QuickMenuExpando;
            var quickMenuRoot = QuickMenu.prop_QuickMenu_0.gameObject;
            
            var fullMenuExpandoPrefab = myStuffBundle.BigMenuExpando;
            var fullMenuRoot = VRCUiManager.field_Protected_Static_VRCUiManager_0.menuContent;
            
            foreach (var valueTuple in GameObjectToCategoryList)
            {
                var categoryEnum = valueTuple.Item1;
                var gameObjectPath = valueTuple.Item2;
                var isBigMenu = valueTuple.Item3;

                var gameObject = GameObject.Find(gameObjectPath);
                if (gameObject == null)
                {
                    MelonModLogger.LogError($"GameObject at path {gameObjectPath} for category {categoryEnum} was not found, not decorating");
                    continue;
                }

                if (isBigMenu)
                {
                    var expando = Object.Instantiate(fullMenuExpandoPrefab, fullMenuRoot.transform, false);
                    myMenuRoots[categoryEnum] = expando;
                    var expandoTransform = expando.transform;
                    expandoTransform.localScale = Vector3.one * 2;
                    expandoTransform.localPosition = new Vector3(-775, -435, -10);
                    expando.AddComponent<VRC_UiShape>();
                    expando.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                    {
                        var compo = expando.GetComponent<VerticalLayoutGroup>();
                        compo.childAlignment = compo.childAlignment == TextAnchor.LowerLeft
                            ? TextAnchor.LowerRight
                            : TextAnchor.LowerLeft;
                    }));
                    
                    myVisibilityTransfers.Add((gameObject, expando));
                    
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
                    transform.Cast<RectTransform>().localPosition = new Vector3(55, -700, 5);

                    expando.AddComponent<VRC_UiShape>();
                    
                    myVisibilityTransfers.Add((gameObject, expando));
                    
                    FillQuickMenuExpando(expando, categoryEnum);
                    
                    SetLayerRecursively(expando, quickMenuRoot.layer);
                }
            }
        }

        private void FillBigMenuExpando(GameObject expando, ExpandedMenu categoryEnum)
        {
            var expandoRoot = expando.transform.Find("Content").Cast<RectTransform>();
            
            expandoRoot.DestroyChildren();

            var clickButtonPrefab = myStuffBundle.QuickMenuButton;

            if (ExpansionKitApi.RegisteredButtons.TryGetValue(categoryEnum, out var registrations))
            {
                myHasContents[expando] = true;
                foreach (var buttonRegistration in registrations)
                {
                    if (buttonRegistration.Prefab != null)
                    {
                        var newObject = Object.Instantiate(buttonRegistration.Prefab, expandoRoot, false);
                        buttonRegistration.InstanceConsumer?.Invoke(newObject);
                    }
                    else
                    {
                        var buttonInstance = Object.Instantiate(clickButtonPrefab, expandoRoot, false);
                        buttonInstance.GetComponentInChildren<Text>().text = buttonRegistration.Text;
                        buttonInstance.GetComponent<Button>().onClick.AddListener(buttonRegistration.Action);
                        buttonRegistration.InstanceConsumer?.Invoke(buttonInstance);
                    }
                }
            }
        }

        private void DecorateFullMenu()
        {
            var fullMenuRoot = VRCUiManager.field_Protected_Static_VRCUiManager_0.menuContent;

            var settingsExpandoPrefab = myStuffBundle.SettingsMenuExpando;
            myModSettingsExpando = Object.Instantiate(settingsExpandoPrefab, fullMenuRoot.transform, false);
            myModSettingsExpandoTransform = myModSettingsExpando.transform;
            myModSettingsExpandoTransform.localScale = Vector3.one * 1.52f;
            myModSettingsExpandoTransform.localPosition = new Vector3(-755, -550, -10);
            myModSettingsExpando.AddComponent<VRC_UiShape>();

            ModSettingsHandler.Initialize(myStuffBundle);
            var settingsContentRoot = myModSettingsExpando.transform.Find("Content/Scroll View/Viewport/Content").Cast<RectTransform>();
            MelonCoroutines.Start(ModSettingsHandler.PopulateSettingsPanel(settingsContentRoot));
            
            myVisibilityTransfers.Add((fullMenuRoot.transform.Find("Screens/Settings").gameObject, myModSettingsExpando));
            myHasContents[myModSettingsExpando] = true;

            myModSettingsExpandoTransform.Find("Content/ApplyButton").GetComponent<Button>().onClick.AddListener(new Action(ModPrefs.SaveConfig));

            myModSettingsExpandoTransform.Find("Content/RefreshButton").GetComponent<Button>().onClick
                .AddListener(new Action(() => MelonCoroutines.Start(ModSettingsHandler.PopulateSettingsPanel(settingsContentRoot))));
            
            SetLayerRecursively(myModSettingsExpando, fullMenuRoot.layer);
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (var o in obj.transform) 
                SetLayerRecursively(o.Cast<Transform>().gameObject, layer);
        }

        private void FillQuickMenuExpando(GameObject expando, ExpandedMenu expandedMenu)
        {
            var expandoRoot = expando.transform.Find("Content/Scroll View/Viewport/Content").Cast<RectTransform>();
            
            expandoRoot.DestroyChildren();

            var clickButtonPrefab = myStuffBundle.QuickMenuButton;
            var toggleButtonPrefab = myStuffBundle.QuickMenuToggle;

            myHasContents[expando] = false;

            if (ExpansionKitApi.RegisteredButtons.TryGetValue(expandedMenu, out var registrations))
            {
                foreach (var buttonRegistration in registrations)
                {
                    if (buttonRegistration.Prefab != null)
                    {
                        var newObject = Object.Instantiate(buttonRegistration.Prefab, expandoRoot, false);
                        buttonRegistration.InstanceConsumer?.Invoke(newObject);
                    }
                    else
                    {
                        var buttonInstance = Object.Instantiate(clickButtonPrefab, expandoRoot, false);
                        buttonInstance.GetComponentInChildren<Text>().text = buttonRegistration.Text;
                        buttonInstance.GetComponent<Button>().onClick.AddListener(buttonRegistration.Action);
                        buttonRegistration.InstanceConsumer?.Invoke(buttonInstance);
                    }
                }
                
                myHasContents[expando] = true;
            }

            if (expandedMenu != ExpandedMenu.QuickMenu) return;
            
            foreach (var (category, name) in ExpansionKitSettings.ListPinnedPrefs(false))
            {
                if (!ModPrefs.GetPrefs().TryGetValue(category, out var categoryMap)) continue;
                if (!categoryMap.TryGetValue(name, out var prefDesc)) continue;

                var toggleButton = Object.Instantiate(toggleButtonPrefab, expandoRoot, false);
                toggleButton.GetComponentInChildren<Text>().text = prefDesc.DisplayText ?? name;
                var toggle = toggleButton.GetComponent<Toggle>();
                toggle.isOn = ModPrefs.GetBool(category, name);
                toggle.onValueChanged.AddListener(new Action<bool>(isOn =>
                {
                    prefDesc.ValueEdited = isOn.ToString().ToLowerInvariant();
                    ModPrefs.SaveConfig();
                }));
                
                myHasContents[expando] = true;
            }
            
            SetLayerRecursively(myModSettingsExpando, QuickMenu.prop_QuickMenu_0.gameObject.layer);
        }
    }
}