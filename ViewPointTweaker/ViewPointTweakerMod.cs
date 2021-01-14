using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using MelonLoader.TinyJSON;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;
using ViewPointTweaker;

[assembly:MelonInfo(typeof(ViewPointTweakerMod), "View Point Tweaker", "1.0.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace ViewPointTweaker
{
    public class ViewPointTweakerMod : MelonMod
    {
        private const string ViewPointsFilePath = "UserData/ViewPoints.json";
        
        private static Vector3 ourCurrentDefaultOffset;
        private static Transform ourCurrentHeadOffsetTransform;
        private static Dictionary<string, (float X, float Y, float Z)> ourSavedViewpoints =
            new Dictionary<string, (float X, float Y, float Z)>();
        
        private bool myHighPrecisionMoves;
        
        public override void OnApplicationStart()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UiElementsQuickMenu).AddSimpleButton("Tweak view point", ShowViewpointMenu);

            harmonyInstance.Patch(typeof(IKHeadAlignment)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Single(it => it.GetCustomAttribute<CallerCountAttribute>().Count > 0),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(ViewPointTweakerMod),
                    nameof(HeadAlignmentInitPatch))));
            
            LoadViewpoints();
        }

        private void SaveViewpoints()
        {
            File.WriteAllText(ViewPointsFilePath, JSON.Dump(ourSavedViewpoints, EncodeOptions.NoTypeHints | EncodeOptions.PrettyPrint));
        }

        private void LoadViewpoints()
        {
            if (!File.Exists(ViewPointsFilePath)) return;

            var json = File.ReadAllText(ViewPointsFilePath);
            JSON.MakeInto(JSON.Load(json), out ourSavedViewpoints);
            
            MelonLogger.Log($"Loaded {ourSavedViewpoints.Count} saved viewpoints");
        }

        private static void HeadAlignmentInitPatch(IKHeadAlignment __instance)
        {
            var player = __instance.GetComponentInParent<VRCPlayer>();
            var localPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;

            if (localPlayer == null || player != localPlayer)
                return;
            
            var xform = __instance.transform;
            ourCurrentDefaultOffset = xform.localPosition;
            ourCurrentHeadOffsetTransform = xform;

            var avatarId = localPlayer.prop_ApiAvatar_0?.id;
            if (avatarId == null) return;
            if (ourSavedViewpoints.TryGetValue(avatarId, out var triple))
                xform.localPosition = new Vector3(triple.X, triple.Y, triple.Z);
            
            if (Imports.IsDebugMode())
                MelonLogger.Log("Head alignment set hook");
        }

        private void ShowViewpointMenu()
        {
            var menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(ball.GetComponent<Collider>());

            var ballXform = ball.transform;
            ballXform.localScale *= 0.01f;
            ballXform.SetParent(ourCurrentHeadOffsetTransform.parent);
            ballXform.localPosition = Vector3.zero;
            ball.layer = 18;

            menu.OnContentRootCreated += go =>
            {
                (go.GetComponent<EnableDisableListener>() ?? go.AddComponent<EnableDisableListener>()).OnDisabled +=
                    () =>
                    {
                        if (Imports.IsDebugMode())
                            MelonLogger.Log("Menu closed, cleaning up");
                        Object.Destroy(ball);
                        
                        var avatarId = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_ApiAvatar_0.id;
                        var localPosition = ourCurrentHeadOffsetTransform.localPosition;

                        if (localPosition != ourCurrentDefaultOffset)
                            ourSavedViewpoints[avatarId] = (localPosition.x, localPosition.y, localPosition.z);
                        else
                            ourSavedViewpoints.Remove(avatarId);

                        SaveViewpoints();
                    };
            };

            Text xLabel = null;
            Text yLabel = null;
            Text zLabel = null;

            void DoMove(Vector3 direction)
            {
                var localPosition = ourCurrentHeadOffsetTransform.localPosition + direction * (myHighPrecisionMoves ? 0.001f : 0.01f);
                ourCurrentHeadOffsetTransform.localPosition = localPosition;

                xLabel.text = $"X:\n{localPosition.x:F3}";
                yLabel.text = $"Y:\n{localPosition.y:F3}";
                zLabel.text = $"Z:\n{localPosition.z:F3}";
            }
            
            menu.AddSimpleButton("Up", () => DoMove(Vector3.up));
            menu.AddSimpleButton("Forward", () => DoMove(Vector3.forward));
            menu.AddSimpleButton("Left", () => DoMove(Vector3.left));
            menu.AddToggleButton("High precision", b => myHighPrecisionMoves = b, () => myHighPrecisionMoves);

            menu.AddSimpleButton("Down", () => DoMove(Vector3.down));
            menu.AddSimpleButton("Back", () => DoMove(Vector3.back));
            menu.AddSimpleButton("Right", () => DoMove(Vector3.right));
            menu.AddSpacer();

            menu.AddLabel("Local\ncoords:", o => o.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter);
            menu.AddLabel("X:", o => (xLabel = o.GetComponentInChildren<Text>()).alignment = TextAnchor.MiddleCenter);
            menu.AddLabel("Y:", o => (yLabel = o.GetComponentInChildren<Text>()).alignment = TextAnchor.MiddleCenter);
            menu.AddLabel("Z:", o => (zLabel = o.GetComponentInChildren<Text>()).alignment = TextAnchor.MiddleCenter);

            menu.AddSimpleButton("Reset", () =>
            {
                ourCurrentHeadOffsetTransform.localPosition = ourCurrentDefaultOffset;
                DoMove(Vector3.zero);
            });
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Back", menu.Hide);
            
            menu.Show();
            
            DoMove(Vector3.zero);
        }
    }
}