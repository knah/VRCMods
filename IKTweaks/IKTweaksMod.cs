using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IKTweaks;
using MelonLoader;
using RootMotionNew.FinalIK;
using UIExpansionKit.API;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Delegate = Il2CppSystem.Delegate;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(IKTweaksMod), "IKTweaks", "1.0.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace IKTweaks
{
    public class IKTweaksMod : MelonMod
    {
        private static readonly Queue<Action> ourToMainThreadQueue = new Queue<Action>();

        public override void OnApplicationStart()
        {
            IkTweaksSettings.RegisterSettings();

            BundleHolder.Init();

            ClassInjector.RegisterTypeInIl2Cpp<VRIK_New>();
            ClassInjector.RegisterTypeInIl2Cpp<TwistRelaxer_New>();

            VrIkHandling.HookVrIkInit(harmonyInstance);
            FullBodyHandling.HookFullBodyController(harmonyInstance);
            
            Camera.onPreRender = Delegate.Combine(Camera.onPreRender, (Camera.CameraCallback) OnVeryLateUpdate).Cast<Camera.CameraCallback>();
            
            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit" && !it.Info.Version.StartsWith("0.1."))) 
                AddUixActions();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddUixActions()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("More IKTweaks...", ShowIKTweaksMenu);
        }

        private static void ShowIKTweaksMenu()
        {
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            menu.AddSimpleButton("Clear per-avatar stored calibrations", CalibrationManager.ClearNonUniversal);
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Open documentation in browser", () => Process.Start("https://github.com/knah/VRCMods#iktweaks"));
            
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Close", menu.Hide);
            
            menu.Show();
        }

        public override void VRChat_OnUiManagerInit()
        {
            var calibrateButton = GameObject.Find("UserInterface/QuickMenu/ShortcutMenu/CalibrateButton")
                .GetComponent<Button>();
            var oldCalibrate = calibrateButton.onClick;
            calibrateButton.onClick = new Button.ButtonClickedEvent();
            calibrateButton.onClick.AddListener(new Action(() =>
            {
                if (IkTweaksSettings.FullBodyVrIk)
                {
                    if (IkTweaksSettings.CalibrateUseUniversal)
                        CalibrationManager.Clear();
                    else
                        CalibrationManager.Clear(VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_ApiAvatar_0.id);
                }

                oldCalibrate.Invoke();
            }));

            var steamVrControllerManager = CalibrationManager.GetControllerManager();
            var puckPrefab = steamVrControllerManager.objects.First(it =>
                it != steamVrControllerManager.left && it != steamVrControllerManager.right);
            var newPucks = new Il2CppReferenceArray<GameObject>(5 + 6);
            var newUints = new Il2CppStructArray<uint>(5 + 6);
            for (var i = 0; i < 5; i++)
            {
                newPucks[i] = steamVrControllerManager.objects[i];
                newUints[i] = steamVrControllerManager.field_Private_ArrayOf_UInt32_0[i];
            }

            var trackersParent = puckPrefab.transform.parent;
            for (var i = 0; i < 6; i++)
            {
                var newPuck = Object.Instantiate(puckPrefab, trackersParent, true);
                newPuck.name = "Puck" + (i + 4);
                newPuck.GetComponent<SteamVR_TrackedObject>().index = SteamVR_TrackedObject.EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique.None;
                newPuck.SetActive(false);
                newPucks[i + 5] = newPuck;
                newUints[i + 5] = UInt32.MaxValue;
            }

            steamVrControllerManager.objects = newPucks;
            steamVrControllerManager.field_Private_ArrayOf_UInt32_0 = newUints;

            steamVrControllerManager.Method_Private_Void_VREvent_t_PDM_1(new VREvent_t());
        }

        private static bool ourHadUpdateThisFrame = false;
        public override void OnUpdate()
        {
            VrIkHandling.Update();
            FullBodyHandling.Update();
            ourHadUpdateThisFrame = false;
        }

        public void OnVeryLateUpdate(Camera _)
        {
            if (ourHadUpdateThisFrame) return;
            
            ourHadUpdateThisFrame = true;

            var toRun = ourToMainThreadQueue.ToList();
            ourToMainThreadQueue.Clear();
            
            foreach (var action in toRun)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    MelonLogger.LogError(ex.ToString());
                }
            }
        }

        public static YieldVeryLateUpdateAwaitable AwaitVeryLateUpdate()
        {
            return new YieldVeryLateUpdateAwaitable();
        }

        public struct YieldVeryLateUpdateAwaitable : INotifyCompletion
        {
            public bool IsCompleted => false;

            public YieldVeryLateUpdateAwaitable GetAwaiter() => this;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                ourToMainThreadQueue.Enqueue(continuation);
            }
        }

        public override void OnModSettingsApplied()
        {
            IkTweaksSettings.OnSettingsApplied();
        }
    }
}