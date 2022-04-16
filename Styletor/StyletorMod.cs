using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Styletor;
using Styletor.ExtraHandlers;
using Styletor.Styles;
using UIExpansionKit;
using UIExpansionKit.API;
using UnhollowerRuntimeLib.XrefScans;
using VRC.UI.Core.Styles;

[assembly:MelonInfo(typeof(StyletorMod), "Styletor", "0.3.1", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

#nullable disable

namespace Styletor
{
    public partial class StyletorMod : MelonMod
    {
        public static StyletorMod Instance;
        
        private StyleEngineWrapper myStyleEngine;
        private StylesLoader myStylesLoader;

        private SettingsHolder mySettings;

        private UiLasersHandler myLasersHandler;
        private ActionMenuHandler myActionMenuHandler;
        
        public SettingsHolder Settings => mySettings;

        public override void OnApplicationStart()
        {
            Instance = this;

            Directory.CreateDirectory(Path.Combine(MelonUtils.UserDataDirectory, StylesLoader.StylesSubDir));
            
            mySettings = new SettingsHolder();

            var settingMenu = ExpansionKitApi.GetSettingsCategory(SettingsHolder.CategoryIdentifier);
            
            settingMenu.AddSimpleButton("Configure mix-in styles", ShowMixinMenu);
            settingMenu.AddSimpleButton("Reload styles from disk", ReloadStyles);
            settingMenu.AddSimpleButton("Export default VRChat style reference", ExportStyleClick);
            settingMenu.AddSimpleButton("Export QM object/style tree", ExportTreeClick);
            settingMenu.AddSimpleButton("Open mod documentation in browser", () => Process.Start("https://github.com/knah/VRCMods#Styletor"));

            MelonCoroutines.Start(WaitForStyleInit());
        }

        private void ExportStyleClick()
        {
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList.With(numRows: 4, rowHeight: 100));

            if (myStyleEngine == null)
            {
                menu.AddLabel("Styles are not loaded yet. Open quick menu once and try again!");
                menu.AddSpacer();
                menu.AddSpacer();
            }
            else
            {
                menu.AddLabel("Are you sure you want to export the default UI skin? This process will freeze your game for a bit. It will be available in UserData/StyletorDefaultSkin.");
                menu.AddSpacer();
                menu.AddSimpleButton("Yep, export it! No freeze can stop me!", DoSkinExport);
            }
            
            menu.AddSimpleButton("Close", menu.Hide);
            
            menu.Show();
        }

        private void DoSkinExport()
        {
            BuiltinStyleExporter.ExportDefaultStyle(Path.Combine(MelonUtils.UserDataDirectory, "StyletorDefaultSkin"), myStyleEngine.StyleEngine);
        }
        
        private void ExportTreeClick()
        {
            if (myStyleEngine == null) return;
            BuiltinStyleExporter.ExportObjectTree(myStyleEngine.StyleEngine.gameObject, Path.Combine(MelonUtils.UserDataDirectory, "StyletorDefaultSkin", "tree.txt"));
        }

        private void ShowMixinMenu()
        {
            var disabledMixinSet = mySettings.DisabledMixinsEntry.Value.Split('|').ToHashSet();
            
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            menu.AddLabel("Click on a mix-in style to toggle it");
            
            foreach (var (id, name) in myStylesLoader.GetKnownMixIns())
            {
                menu.AddSimpleButton($"{name} ({(disabledMixinSet.Contains(id) ? "Disabled" : "Enabled")})", () =>
                {
                    if (disabledMixinSet.Contains(id))
                        disabledMixinSet.Remove(id);
                    else
                        disabledMixinSet.Add(id);

                    mySettings.DisabledMixinsEntry.Value = disabledMixinSet.Join(delimiter: "|");
                    menu.Hide();
                    ShowMixinMenu();
                });
            }
            
            menu.AddSimpleButton("Close", menu.Hide);
            menu.Show();
        }

        private IEnumerator WaitForStyleInit()
        {
            while (myStyleEngine == null)
            {
                yield return null;
                var qmHolder = UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/Canvas_QuickMenu(Clone)");
                if (qmHolder == null) continue;
                var styleEngine = qmHolder.GetComponent<StyleEngine>();
                if (styleEngine != null)
                    myStyleEngine = new StyleEngineWrapper(styleEngine);
            }

            var initCandidates = typeof(StyleEngine).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(it => it.Name.StartsWith("Method_Public_Void_") && it.GetParameters().Length == 0 && !it.Name.Contains("_PDM_")).ToList();

            var initMethods = initCandidates.Where(it =>
                XrefScanner.XrefScan(it).Any(jt =>
                    jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "style-sheet")).ToList();

            var initMethod = initMethods.Count == 1 ? initMethods[0] : null;

            if (initMethod == null) 
                MelonLogger.Warning("No Init method on StyleEngine, will wait for natural init");
            else
                initMethod.Invoke(myStyleEngine.StyleEngine, Array.Empty<object>());

            while (myStyleEngine.StyleEngine.field_Private_List_1_ElementStyle_0.Count == 0)
                yield return null;

            myStyleEngine.BackupDefaultStyle();

            myStylesLoader = new StylesLoader(myStyleEngine, mySettings);

            try
            {
                myLasersHandler = new UiLasersHandler(mySettings);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"UI Laser recoloring handler failed to initialize: {ex}");
            }
            
            try
            {
                myActionMenuHandler = new ActionMenuHandler(mySettings);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Action Menu recoloring handler failed to initialize: {ex}");
            }
        }

        public void ReloadStyles()
        {
            myStylesLoader?.ReloadStyles();
        }
    }
}