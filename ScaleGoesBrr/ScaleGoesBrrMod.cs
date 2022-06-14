using System;
using System.Collections;
using HarmonyLib;
using MelonLoader;
using RootMotion.FinalIK;
using ScaleGoesBrr;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.SDKBase;

[assembly:MelonInfo(typeof(ScaleGoesBrrMod), "Scale Goes Brr", "1.1.3", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace ScaleGoesBrr
{
    public partial class ScaleGoesBrrMod : MelonMod
    {
        private static MelonPreferences_Entry<bool> ourIsEnabled;
        private static MelonPreferences_Entry<bool> ourFixFlyOff;
        internal static MelonPreferences_Entry<bool> FixPlayspaceCenterBias;
        internal static MelonPreferences_Entry<bool> DoShoulderScaling;

        private static VRCVrCameraSteam ourSteamCamera;
        private static Transform ourCameraTransform;

        public static event Action<Transform, float> OnAvatarScaleChanged;

        internal static void FireScaleChange(Transform avatarRoot, float newScale)
        {
            OnAvatarScaleChanged?.Invoke(avatarRoot, newScale);
        }

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ScaleGoesBrrComponent>();

            var category = MelonPreferences.CreateCategory("ScaleGoesBrr", "Scale Goes Brr");
            ourIsEnabled = category.CreateEntry("Enabled", true, "Enable avatar scaling support");
            FixPlayspaceCenterBias = category.CreateEntry("FixPlayspaceCenterBias", true, "Scale towards avatar root (not playspace center)");
            ourFixFlyOff = category.CreateEntry("FixFlyOff", true, "Fix avatar root flying off");
            DoShoulderScaling = category.CreateEntry("DoShoulderScaling", true, "Scale avatar shoulders (with elbow trackers)");
        
            HarmonyInstance.Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.Start)),
                postfix: new HarmonyMethod(typeof(ScaleGoesBrrMod), nameof(PlayerStartPatch)));
            
            DoAfterUiManagerInit(OnUiManagerInit);
        }

        private void OnUiManagerInit()
        {
            foreach (var vrcTracking in VRCTrackingManager.field_Private_Static_VRCTrackingManager_0
                         .field_Private_List_1_VRCTracking_0)
            {
                var trackingSteam = vrcTracking.TryCast<VRCTrackingSteam>();
                if (trackingSteam == null) continue;

                ourSteamCamera = trackingSteam.GetComponentInChildren<VRCVrCameraSteam>();
                ourCameraTransform = trackingSteam.transform.Find("SteamCamera/[CameraRig]/Neck/Camera (head)/Camera (eye)");
                
                return;
            }
        }

        internal static void UpdateCameraOffsetForScale(Vector3 offset)
        {
            ourSteamCamera.field_Private_Vector3_0 = -offset / ourSteamCamera.transform.lossyScale.x;
            if (ourCameraTransform != null)
                ourCameraTransform.localPosition = -offset / ourCameraTransform.lossyScale.x;
        }

        private static IEnumerator WaitForPlayerBrr(VRCPlayer player)
        {
            while (player != null && player.field_Private_VRCPlayerApi_0 == null)
                yield return null;
            
            if (player == null || !player.prop_VRCPlayerApi_0.isLocal) yield break;
            
            player.field_Private_VRCAvatarManager_0.field_Private_AvatarCreationCallback_0 += new Action<GameObject, VRC_AvatarDescriptor, bool>(OnLocalPlayerAvatarCreated);
            var avatar = player.transform.Find("ForwardDirection/Avatar");
            if (avatar != null)
                OnLocalPlayerAvatarCreated(avatar.gameObject, avatar.GetComponent<VRC_AvatarDescriptor>(), false);
        }

        private static void PlayerStartPatch(VRCPlayer __instance)
        {
            MelonCoroutines.Start(WaitForPlayerBrr(__instance));
        }

        private static void OnLocalPlayerAvatarCreated(GameObject go, VRC_AvatarDescriptor descriptor, bool unknown)
        {
            OnLocalPlayerAvatarCreatedImpl(go, descriptor);
        }

        internal static void FixAvatarRootFlyingOff(Transform avatarRoot)
        {
            if (!ourFixFlyOff.Value) return;

            avatarRoot.get_localPosition_Injected(out var oldPos);
            oldPos.x = oldPos.z = 0;
            avatarRoot.localPosition = oldPos;
        }

        private static IEnumerator OnLocalPlayerAvatarCreatedCoro(Vector3 originalScale, GameObject go)
        {
            var trackingRoot = VRCTrackingManager.field_Private_Static_VRCTrackingManager_0.transform;
            var uiRoot = GameObject.Find("/UserInterface").transform;
            var unscaledUi = uiRoot.Find("UnscaledUI");
            
            // give it 3 frames for VRCTrackingManager to unbamboozle itself
            for (var i = 0; i < 3 && go != null; i++)
            {
                MelonDebug.Msg($"Funny numbers go brr: {i} {VRCTrackingManager.field_Private_Static_Vector3_0.ToString()} {VRCTrackingManager.field_Private_Static_Vector3_1.ToString()}");
                MelonDebug.Msg($"Scale stuff: a={go.transform.localScale.y} t={trackingRoot.localScale.y}");
                MelonDebug.Msg($"UI stuff: r={uiRoot.localScale.y} u={unscaledUi.localScale.y}");
                yield return null;
            }

            if (go == null) yield break;

            var originalTrackingRootScale = trackingRoot.localScale;

            MelonLogger.Msg($"Initialized scaling support for current avatar: avatar initial scale {originalScale.y}, tracking initial scale {originalTrackingRootScale.y}");

            var comp = go.AddComponent<ScaleGoesBrrComponent>();
            comp.source = go.transform;
            comp.RootFix = comp.source.parent;
            comp.targetPs = trackingRoot;
            comp.targetAl = ourSteamCamera.GetComponentInChildren<AudioListener>().transform;
            comp.targetAl.get_localScale_Injected(out comp.originalTargetAlScale);
            comp.originalSourceScale = originalScale;
            comp.originalTargetPsScale = originalTrackingRootScale;
            comp.targetUi = uiRoot.transform;
            comp.targetUiInverted = unscaledUi;

            var ikSolverVR = go.GetComponent<VRIK>().solver;
            comp.locomotion = ikSolverVR.locomotion;
            comp.originalStep = comp.locomotion.footDistance;

            comp.targetLeftArm = ikSolverVR.leftArm;
            comp.targetRightArm = ikSolverVR.rightArm;
            comp.leftArmOriginalShoulder = ikSolverVR.leftArm.vrcShoulderHeightAboveChest;
            comp.rightArmOriginalShoulder = ikSolverVR.rightArm.vrcShoulderHeightAboveChest;

            comp.ActuallyDoThings = true;

            var avatarManager = go.GetComponentInParent<VRCAvatarManager>();
            comp.avatarManager = avatarManager;
            comp.amSingle0 = avatarManager.field_Private_Single_0;
            comp.amSingle1 = avatarManager.field_Private_Single_1;
            comp.amSingle3 = avatarManager.field_Private_Single_3;
            comp.amSingle4 = avatarManager.field_Private_Single_4;
            comp.amSingle5 = avatarManager.field_Private_Single_5;
            comp.amSingle6 = avatarManager.field_Private_Single_6;
            comp.amSingle7 = avatarManager.field_Private_Single_7;

            comp.targetVp = avatarManager.field_Private_VRC_AnimationController_0
                .GetComponentInChildren<IKHeadAlignment>().transform;
            comp.targetVpParent = comp.targetVp.parent;

            // hand effector scaling is used for IKTweaks compat
            comp.targetHandParentL = avatarManager.field_Internal_IkController_0.transform.Find("LeftEffector");
            comp.targetHandParentR = avatarManager.field_Internal_IkController_0.transform.Find("RightEffector");

            comp.tmSV0 = VRCTrackingManager.field_Private_Static_Vector3_0;
            comp.tmSV1 = VRCTrackingManager.field_Private_Static_Vector3_1;
            comp.tmSV1 = VRCTrackingManager.field_Private_Static_Vector3_2;
            comp.tmReady = true;
        }

        private static void OnLocalPlayerAvatarCreatedImpl(GameObject go, VRC_AvatarDescriptor descriptor)
        {
            if (!ourIsEnabled.Value) return;
            
            if (descriptor == null || descriptor.TryCast<VRCSDK2.VRC_AvatarDescriptor>() != null)
            {
                MelonLogger.Msg("Current avatar is SDK2, ignoring rescaling support");
                return;
            }

            var originalScale = go.transform.localScale;
            
            MelonCoroutines.Start(OnLocalPlayerAvatarCreatedCoro(originalScale, go));
        }

    }
}