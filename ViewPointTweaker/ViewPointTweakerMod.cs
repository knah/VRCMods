using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using MelonLoader.TinyJSON;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.UI;
using ViewPointTweaker;

[assembly:MelonInfo(typeof(ViewPointTweakerMod), "View Point Tweaker", "1.0.5", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace ViewPointTweaker
{
    internal partial class ViewPointTweakerMod : MelonMod
    {
        private const string ViewPointsFilePath = "UserData/ViewPoints.json";
        
        private static Vector3 ourCurrentDefaultOffset;
        private static Transform ourCurrentHeadOffsetTransform;
        private static Dictionary<string, (float X, float Y, float Z)> ourSavedViewpoints =
            new Dictionary<string, (float X, float Y, float Z)>();

        private static VRCVrCameraSteam ourSteamCamera;
        private static Transform ourCameraTransform;
        
        private bool myHighPrecisionMoves;
        
        public override void OnApplicationStart()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UiElementsQuickMenu).AddSimpleButton("Tweak view point", ShowViewpointMenu);

            foreach (var methodInfo in typeof(IKHeadAlignment)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(it => it.GetCustomAttribute<CallerCountAttribute>().Count > 0 && it.GetParameters().Length == 1 && it.GetParameters()[0].ParameterType == typeof(Animator)))
            {
                HarmonyInstance.Patch(methodInfo,
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(ViewPointTweakerMod),
                        nameof(HeadAlignmentInitPatch))));
            }
            
            LoadViewpoints();
            
            DoAfterUiManagerInit(OnUiManagerInit);
        }

        private void OnUiManagerInit()
        {
            foreach (var vrcTracking in VRCTrackingManager.field_Private_Static_VRCTrackingManager_0.field_Private_List_1_VRCTracking_0)
            {
                var trackingSteam = vrcTracking.TryCast<VRCTrackingSteam>();
                if(trackingSteam == null) continue;

                ourSteamCamera = trackingSteam.GetComponentInChildren<VRCVrCameraSteam>();
                ourCameraTransform = trackingSteam.transform.Find("SteamCamera/[CameraRig]/Neck/Camera (head)/Camera (eye)");

                return;
            }
            
            MelonLogger.Error("Steam tracking not found, things will break");
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
            
            MelonLogger.Msg($"Loaded {ourSavedViewpoints.Count} saved viewpoints");
        }

        private static void HeadAlignmentInitPatch(IKHeadAlignment __instance)
        {
            var player = __instance.GetComponentInParent<VRCPlayer>();
            var localPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;

            if (localPlayer == null || player != localPlayer)
                return;

            var avatarManager = localPlayer.prop_VRCAvatarManager_0;
            if (avatarManager == null) return; 
            
            var xform = __instance.transform;
            ourCurrentDefaultOffset = xform.localPosition;
            ourCurrentHeadOffsetTransform = xform;
            
            MelonDebug.Msg($"avatar id: {avatarManager.field_Private_ApiAvatar_0?.id}");

            var avatarId = avatarManager.field_Private_ApiAvatar_0?.id;
            if (avatarId == null) return;
            if (ourSavedViewpoints.TryGetValue(avatarId, out var triple))
            {
                var offset = new Vector3(triple.X, triple.Y, triple.Z);
                SetViewPointOffset(offset);
                MelonCoroutines.Start(SetOffsetAgainLater(offset));
            }
            
            MelonDebug.Msg("Head alignment set hook");
        }

        private static IEnumerator SetOffsetAgainLater(Vector3 offset)
        {
            for (var i = 0; i < 3; i++)
                yield return null;
            
            SetViewPointOffset(offset);
        }

        private static void SetViewPointOffset(Vector3 offset)
        {
            ourCurrentHeadOffsetTransform.localPosition = offset;
            ourSteamCamera.field_Private_Vector3_0 = -offset / ourSteamCamera.transform.lossyScale.x;
            if (ourCameraTransform != null)
                ourCameraTransform.localPosition = -offset / ourCameraTransform.lossyScale.x;
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
                        MelonDebug.Msg("Menu closed, cleaning up");
                        Object.Destroy(ball);
                        
                        var avatarId = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_VRCAvatarManager_0.field_Private_ApiAvatar_0.id;
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
                SetViewPointOffset(localPosition);

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
                SetViewPointOffset(ourCurrentDefaultOffset);
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