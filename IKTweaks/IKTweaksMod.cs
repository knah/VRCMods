using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IKTweaks;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using UIExpansionKit.Components;
using UnityEngine;

[assembly:MelonInfo(typeof(IKTweaksMod), "IKTweaks", "2.1.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace IKTweaks
{
    internal partial class IKTweaksMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            if (!CheckWasSuccessful || !MustStayTrue || MustStayFalse) return;
            
            IkTweaksSettings.RegisterSettings();

            VrIkHandling.HookVrIkInit();

            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit" && !it.Info.Version.StartsWith("0.1."))) 
                AddUixActions();
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddUixActions()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("More IKTweaks...", ShowIKTweaksMenu);

            var settingNameList = new[]
            {
                nameof(IkTweaksSettings.StraightSpineAngle), 
                nameof(IkTweaksSettings.StraightSpinePower), 
                nameof(IkTweaksSettings.DoHipShifting), 
                nameof(IkTweaksSettings.PreStraightenSpine), 
                nameof(IkTweaksSettings.StraightenNeck), 
                nameof(IkTweaksSettings.PinHipRotation), 
                nameof(IkTweaksSettings.NeckPriority), 
                nameof(IkTweaksSettings.SpineRelaxIterations), 
                nameof(IkTweaksSettings.MaxNeckAngleBack),
                nameof(IkTweaksSettings.MaxNeckAngleFwd),
                nameof(IkTweaksSettings.MaxSpineAngleBack),
                nameof(IkTweaksSettings.MaxSpineAngleFwd),
            };
            var updateCallbacks = new List<Action>();
            
            foreach (var s in settingNameList)
                updateCallbacks.Add(ExpansionKitApi.RegisterSettingsVisibilityCallback(
                    IkTweaksSettings.IkTweaksCategory, s, () => IkTweaksSettings.FullBodyVrIk.Value));

            IkTweaksSettings.FullBodyVrIk.OnValueChangedUntyped += () =>
            {
                foreach (var it in updateCallbacks) it();
            };
        }

        private static void ShowIKTweaksMenu()
        {
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Open documentation in browser", () => Process.Start("https://github.com/knah/VRCMods#iktweaks"));
            menu.AddSpacer();

            menu.AddSimpleButton("Adjust hand offsets",
                () => ShowHandsCalibrationMenu(IkTweaksSettings.HandPositionOffset, IkTweaksSettings.DefaultHandOffset,
                    0.001f, "Offsets:", (_, newOff) => VrIkHandling.HandOffsetsManager?.UpdatePositionOffset(_, newOff)));
            menu.AddSimpleButton("Adjust hand angles",
                () => ShowHandsCalibrationMenu(IkTweaksSettings.HandAngleOffset, IkTweaksSettings.DefaultHandAngle, 1,
                    "Angles:", (_, newRot) => VrIkHandling.HandOffsetsManager?.UpdateRotationOffset(_, newRot)));
            menu.AddSpacer();
            menu.AddSimpleButton("Close", menu.Hide);
            
            menu.Show();
        }

        private static void ShowHandsCalibrationMenu(MelonPreferences_Entry<Vector3> entry, Vector3 defaultValue, float moveStep, string label, Action<Vector3, Vector3> apply)
        {
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.QuickMenu4Columns);

            var highPrecisionMoves = true;
            var offset = entry.Value;
            var prevOffset = offset;
            
            void CommitOffset()
            {
                apply(prevOffset, offset);
                prevOffset = offset;
            }
            
            menu.OnContentRootCreated += go =>
            {
                (go.GetComponent<EnableDisableListener>() ?? go.AddComponent<EnableDisableListener>()).OnDisabled +=
                    () =>
                    {
                        MelonDebug.Msg("Menu closed, cleaning up");

                        entry.Value = offset;
                        IkTweaksSettings.Category.SaveToFile();
                    };
            };

            IMenuLabel xLabel = null;
            IMenuLabel yLabel = null;
            IMenuLabel zLabel = null;

            void DoMove(Vector3 direction)
            {
                offset += direction * (highPrecisionMoves ? moveStep : moveStep * 10);
                CommitOffset();

                xLabel.SetText($"X:\n{offset.x:F3}");
                yLabel.SetText($"Y:\n{offset.y:F3}");
                zLabel.SetText($"Z:\n{offset.z:F3}");
            }
            
            menu.AddSimpleButton("+Y", () => DoMove(Vector3.up));
            menu.AddSimpleButton("+Z", () => DoMove(Vector3.forward));
            menu.AddSimpleButton("+X", () => DoMove(Vector3.right));
            menu.AddToggleButton("High precision", b => highPrecisionMoves = b, () => highPrecisionMoves);

            menu.AddSimpleButton("-Y", () => DoMove(Vector3.down));
            menu.AddSimpleButton("-Z", () => DoMove(Vector3.back));
            menu.AddSimpleButton("-X", () => DoMove(Vector3.left));
            menu.AddSpacer();

            menu.AddLabel(label).SetAnchor(TextAnchor.MiddleCenter);
            xLabel = menu.AddLabel($"X:\n{offset.x:F3}").SetAnchor(TextAnchor.MiddleCenter);
            yLabel = menu.AddLabel($"Y:\n{offset.y:F3}").SetAnchor(TextAnchor.MiddleCenter);
            zLabel = menu.AddLabel($"Z:\n{offset.z:F3}").SetAnchor(TextAnchor.MiddleCenter);

            menu.AddSimpleButton("Reset", () =>
            {
                offset = defaultValue;
                DoMove(Vector3.zero);
            });
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Back", menu.Hide);
            
            menu.Show(true);
        }
    }
}