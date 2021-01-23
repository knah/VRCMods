using System;
using System.Linq;
using System.Reflection;
using Harmony;
using MelonLoader;
using RootMotion.FinalIK;
using RootMotionNew.FinalIK;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC.Core;
using IKSolverVR = RootMotionNew.FinalIK.IKSolverVR;

namespace IKTweaks
{
    public class FullBodyHandling
    {
        internal static VRCFbbIkController LastInitializedController;

        internal static float LeftElbowWeight = 0f;
        internal static float RightElbowWeight = 0f;
        internal static float LeftKneeWeight = 0f;
        internal static float RightKneeWeight = 0f;
        internal static float ChestWeight = 0f;

        public static bool LastCalibrationWasInCustomIk;
        private static Func<bool> ourIsFbtSupported;
        
        public static void Update()
        {
            if (!IkTweaksSettings.FullBodyVrIk || !LastCalibrationWasInCustomIk) return;

            if (LastInitializedController == null || LastInitializedController.field_Private_FullBodyBipedIK_0 == null) return;
            
            var fbbik = LastInitializedController.field_Private_FullBodyBipedIK_0;
            
            var vrik = fbbik.GetComponent<VRIK_New>();
            
            fbbik.skipSolverUpdate = true;
            if (LastInitializedController.field_Private_FBBIKHeadEffector_0 != null)
            {
                LastInitializedController.field_Private_FBBIKHeadEffector_0.enabled = false;
                LastInitializedController.field_Private_FBBIKArmBending_0.enabled = false;
                LastInitializedController.field_Private_ShoulderRotator_0.enabled = false;
                LastInitializedController.field_Private_TwistRelaxer_0.enabled = false;
                LastInitializedController.field_Private_TwistRelaxer_1.enabled = false;
                    
                // lastInitedController.field_Private_FBBIKHeadEffector_0.positionWeight = 0f;
                // lastInitedController.field_Private_FBBIKHeadEffector_0.rotationWeight = 0f;

                if (vrik == null) return;
                if (!IkTweaksSettings.IgnoreAnimations)
                {
                    vrik.solver.spine.positionWeight = LastInitializedController.field_Private_FBBIKHeadEffector_0.positionWeight;
                    vrik.solver.spine.rotationWeight = LastInitializedController.field_Private_FBBIKHeadEffector_0.rotationWeight;
                }
                else
                {
                    vrik.solver.spine.positionWeight = 1;
                    vrik.solver.spine.rotationWeight = 1;
                }
            }
            
            if (vrik == null) return;

            if (!IkTweaksSettings.IgnoreAnimations)
            {
                vrik.solver.leftLeg.positionWeight = fbbik.solver.leftFootEffector.positionWeight;
                vrik.solver.leftLeg.rotationWeight = fbbik.solver.leftFootEffector.rotationWeight;

                vrik.solver.rightLeg.positionWeight = fbbik.solver.rightFootEffector.positionWeight;
                vrik.solver.rightLeg.rotationWeight = fbbik.solver.rightFootEffector.rotationWeight;

                vrik.solver.spine.pelvisPositionWeight = fbbik.solver.bodyEffector.positionWeight;
                vrik.solver.spine.pelvisRotationWeight = fbbik.solver.bodyEffector.rotationWeight;

                vrik.solver.leftArm.positionWeight = fbbik.solver.leftHandEffector.positionWeight;
                vrik.solver.leftArm.rotationWeight = fbbik.solver.leftHandEffector.rotationWeight;

                vrik.solver.rightArm.positionWeight = fbbik.solver.rightHandEffector.positionWeight;
                vrik.solver.rightArm.rotationWeight = fbbik.solver.rightHandEffector.rotationWeight;
            }
            else
            {
                vrik.solver.leftLeg.positionWeight = 1;
                vrik.solver.leftLeg.rotationWeight = 1;

                vrik.solver.rightLeg.positionWeight = 1;
                vrik.solver.rightLeg.rotationWeight = 1;

                vrik.solver.spine.pelvisPositionWeight = 1;
                vrik.solver.spine.pelvisRotationWeight = 1;

                vrik.solver.leftArm.positionWeight = 1;
                vrik.solver.leftArm.rotationWeight = 1;

                vrik.solver.rightArm.positionWeight = 1;
                vrik.solver.rightArm.rotationWeight = 1;
            }

            vrik.enabled = fbbik.enabled;
            vrik.solver.IKPositionWeight = fbbik.solver.IKPositionWeight;
            
            vrik.solver.spine.maxNeckAngleFwd = IkTweaksSettings.MaxNeckAngleFwd;
            vrik.solver.spine.maxNeckAngleBack = IkTweaksSettings.MaxNeckAngleBack;
            vrik.solver.spine.maxSpineAngleFwd = IkTweaksSettings.MaxSpineAngleFwd;
            vrik.solver.spine.maxSpineAngleBack = IkTweaksSettings.MaxSpineAngleBack;
            vrik.solver.spine.relaxationIterations = IkTweaksSettings.SpineRelaxIterations;
            // vrik.solver.spine.trigPelvisStrength = IkTweaksSettings.SpineBendHipsStrength;
            vrik.solver.spine.neckBendPriority = IkTweaksSettings.NeckPriority;

            if (IkTweaksSettings.UseKneeTrackers)
            {
                vrik.solver.leftLeg.bendGoalWeight = LeftKneeWeight;
                vrik.solver.rightLeg.bendGoalWeight = RightKneeWeight;
                vrik.solver.leftLeg.bendToTargetWeight = vrik.solver.rightLeg.bendToTargetWeight = 0;
            }
            else
            {
                vrik.solver.leftLeg.bendGoalWeight = vrik.solver.rightLeg.bendGoalWeight = 0;
                vrik.solver.leftLeg.bendToTargetWeight = vrik.solver.rightLeg.bendToTargetWeight = 1;
            }
            
            vrik.solver.leftArm.bendGoalWeight = IkTweaksSettings.UseElbowTrackers ? LeftElbowWeight : 0;
            vrik.solver.rightArm.bendGoalWeight = IkTweaksSettings.UseElbowTrackers ? RightElbowWeight : 0;
            vrik.solver.spine.chestGoalWeight = IkTweaksSettings.UseChestTracker ? ChestWeight : 0;

            vrik.solver.spine.hipRotationPinning = IkTweaksSettings.PinHipRotation;
        }
        
        public static void CopyCurrentSettings(VRCFbbIkController source, VRIK_New target)
        {
            var solver = target.solver;
            var sourceSolver = source.field_Private_FullBodyBipedIK_0.solver;
            
            solver.spine.headTarget = source.field_Private_FBBIKHeadEffector_0.transform;
            // solver.leftArm.target = sourceSolver.leftHandEffector.target;
            // solver.rightArm.target = sourceSolver.rightHandEffector.target;

            // MelonLogger.Log($"Current VRIK settings after copy: head {solver.spine.headTarget.name} hips {solver.spine.pelvisTarget.name} rhand {solver.rightArm.target.name} lhand {solver.leftArm.target.name} rleg {solver.rightLeg.target?.name}  lleg {solver.leftLeg.target?.name}");
        }

        private static readonly bool[] BoneResetMask = {true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, false, false,
            false, false, false, false, true, true, true, true, true, true, true, true, true, true, true, true, true,
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            true, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false
        }; 

        internal static void PreSetupVrIk(GameObject targetGameObject)
        {
            var vrik = targetGameObject.GetComponent<VRIK_New>();
            if (vrik is null)
            {
                vrik = targetGameObject.AddComponent<VRIK_New>();
                var animator = targetGameObject.GetComponent<Animator>();
                var handler = new HumanPoseHandler(animator.avatar, animator.transform);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                var muscles = new Il2CppStructArray<float>(HumanTrait.MuscleCount);
                vrik.solver.OnPreUpdate += () =>
                {
                    if (!IkTweaksSettings.IgnoreAnimations || vrik.solver.IKPositionWeight < 0.9f) return;
                    
                    var hipPos = hips.position;
                    var hipRot = hips.rotation;
                    
                    handler.GetHumanPose(out var bodyPos, out var bodyRot, muscles);
                    
                    for (var i = 0; i < muscles.Count; i++)
                        if (BoneResetMask[i])
                            muscles[i] = 0f;

                    handler.SetHumanPose(ref bodyPos, ref bodyRot, muscles);

                    hips.position = hipPos;
                    hips.rotation = hipRot;
                };

                vrik.solver.OnPostUpdate += () =>
                {
                    if (!IkTweaksSettings.AddHumanoidPass) return;
                    
                    var hipPos = hips.position;
                    var hipRot = hips.rotation;

                    handler.GetHumanPose(out var bodyPos, out var bodyRot, muscles);

                    handler.SetHumanPose(ref bodyPos, ref bodyRot, muscles);

                    hips.position = hipPos;
                    hips.rotation = hipRot;
                };
            }

            vrik.enabled = false;
        }

        public static VRIK_New SetupVrIk(VRCFbbIkController source, GameObject targetGameObject)
        {
            var vrik = targetGameObject.GetComponent<VRIK_New>();
            
            vrik.AutoDetectReferences();
            
            if (!IkTweaksSettings.MapToes)
            {
                vrik.references.leftToes = null;
                vrik.references.rightToes = null;
                vrik.solver.leftLeg.UnInitiate();
                vrik.solver.rightLeg.UnInitiate();
            }
            
            vrik.PublicInitiateSolver();

            vrik.enabled = true;
            vrik.solver.plantFeet = false;
            
            vrik.solver.spine.bodyPosStiffness = 0.55f;
            vrik.solver.spine.bodyRotStiffness = 0.2f;
            vrik.solver.spine.neckStiffness = 0.2f;
            vrik.solver.spine.chestClampWeight = 0.5f;
            vrik.solver.spine.headClampWeight = 0.9f;
            vrik.solver.spine.maintainPelvisPosition = 0.00f;
            vrik.solver.spine.minHeadHeight = -100f;

            vrik.solver.spine.maxRootAngle = 180;

            AddTwistRelaxer(vrik.references.leftForearm, vrik, vrik.references.leftHand);
            AddTwistRelaxer(vrik.references.rightForearm, vrik, vrik.references.rightHand);

            vrik.solver.leftArm.shoulderRotationMode = (IKSolverVR.Arm.ShoulderRotationMode) IkTweaksSettings.ShoulderMode;
            vrik.solver.leftArm.shoulderRotationWeight = .8f;
            vrik.solver.rightArm.shoulderRotationMode = (IKSolverVR.Arm.ShoulderRotationMode) IkTweaksSettings.ShoulderMode;
            vrik.solver.rightArm.shoulderRotationWeight = .8f;
            
            vrik.solver.spine.pelvisPositionWeight = 1f;
            vrik.solver.spine.pelvisRotationWeight = 1f;
            vrik.solver.spine.positionWeight = 1f;
            vrik.solver.spine.rotationWeight = 1f;

            vrik.solver.leftLeg.bendToTargetWeight = 0.9f;
            vrik.solver.rightLeg.bendToTargetWeight = 0.9f;

            vrik.solver.locomotion.weight = 0f;
            vrik.solver.leftLeg.positionWeight = 1f;
            vrik.solver.leftLeg.rotationWeight = 1f;
            vrik.solver.rightLeg.positionWeight = 1f;
            vrik.solver.rightLeg.rotationWeight = 1f;
            vrik.solver.leftArm.positionWeight = 1f;
            vrik.solver.leftArm.rotationWeight = 1f;
            vrik.solver.rightArm.positionWeight = 1f;
            vrik.solver.rightArm.rotationWeight = 1f;

            // bestest settings ever
            vrik.solver.spine.bodyPosStiffness = 1f;
            vrik.solver.spine.bodyRotStiffness = 0f;
            vrik.solver.spine.neckStiffness = .5f;
            vrik.solver.spine.rotateChestByHands = .25f;
            vrik.solver.spine.chestClampWeight = 0f;
            vrik.solver.spine.headClampWeight = 0f;
            vrik.solver.spine.maintainPelvisPosition = 0f;
            vrik.solver.spine.minHeadHeight = -100f;
            vrik.solver.spine.moveBodyBackWhenCrouching = 0f;
            // vrik.solver.spine.bodyOffsetWhenNotEvenCrouching = -2f;

            vrik.solver.IKPositionWeight = 1f;

            vrik.solver.leftLeg.bendGoalWeight = vrik.solver.rightLeg.bendGoalWeight = 0.75f;

            // source.field_Private_FullBodyBipedIK_0.solver.IKPositionWeight = 0f;

            source.field_Private_FullBodyBipedIK_0.enabled = false;
            source.field_Private_FBBIKHeadEffector_0.enabled = false;
            source.field_Private_FBBIKArmBending_0.enabled = false;
            source.field_Private_ShoulderRotator_0.enabled = false;
            
            vrik.solver.Reset();

            CopyCurrentSettings(source, vrik);

            return vrik;
        }

        private static void FbbIkInitPrefix(VRCFbbIkController __instance, VRCPlayer? __2, bool __3)
        {
            var vrcPlayer = __2;
            if (vrcPlayer == null) return;
            var isLocalPlayer = vrcPlayer.prop_Player_0?.prop_APIUser_0?.id == APIUser.CurrentUser?.id;
            if(isLocalPlayer != __3) MelonLogger.LogWarning("Computed IsLocal is different from provided");
            if (!isLocalPlayer) return;
            
            LastCalibrationWasInCustomIk = false;
            LastInitializedController = __instance;
        }

        private static void FbbIkInitPostfix(Animator __1, bool __3)
        {
            if (!__3 || !IkTweaksSettings.FullBodyVrIk || !ourIsFbtSupported()) return;
            
            LastCalibrationWasInCustomIk = true;
            CalibrationManager.Calibrate(__1.gameObject);
        }

        private static bool PatchHipAndFeetTracking(ref bool __result)
        {
            if (IkTweaksSettings.DisableFbt)
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static bool LateUpdatePrefix(FullBodyBipedIK __instance)
        {
            if (IkTweaksSettings.FullBodyVrIk && LastCalibrationWasInCustomIk && LastInitializedController.field_Private_FullBodyBipedIK_0 == __instance)
                return false;

            return true;
        }
        
        private static void AddTwistRelaxer(Transform forearm, VRIK_New ik, Transform hand)
        {
            if (forearm == null) return;
            var twistRelaxer = forearm.gameObject.AddComponent<TwistRelaxer_New>();
            twistRelaxer.ik = ik;
            twistRelaxer.weight = 0.5f;
            twistRelaxer.child = hand;
            twistRelaxer.parentChildCrossfade = 0.8f;
        }

        private static bool IsCalibratedForAvatarPrefix(ref bool __result)
        {
            if (IkTweaksSettings.FullBodyVrIk && ourIsFbtSupported())
            {
                __result = true;
                return false;
            }

            return true;
        }

        internal static void HookFullBodyController(HarmonyInstance harmony)
        {
            var fbbIkInit = typeof(VRCFbbIkController).GetMethod(nameof(VRCFbbIkController.Method_Public_Virtual_Final_New_Boolean_VRC_AnimationController_Animator_VRCPlayer_Boolean_0));
            harmony.Patch(fbbIkInit, new HarmonyMethod(typeof(FullBodyHandling), nameof(FbbIkInitPrefix)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(FbbIkInitPostfix)));

            harmony.Patch(AccessTools.Method(typeof(FullBodyBipedIK), nameof(FullBodyBipedIK.LateUpdate)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(LateUpdatePrefix)));
            harmony.Patch(AccessTools.Method(typeof(FullBodyBipedIK), nameof(FullBodyBipedIK.Update)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(LateUpdatePrefix)));
            harmony.Patch(AccessTools.Method(typeof(FullBodyBipedIK), nameof(FullBodyBipedIK.FixedUpdate)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(LateUpdatePrefix)));

            harmony.Patch(AccessTools.Method(typeof(VRCTrackingManager), nameof(VRCTrackingManager.Method_Public_Static_Boolean_String_0)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(IsCalibratedForAvatarPrefix)));
            
            var userOfHfts = typeof(VRCFbbIkController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single(it =>
                    XrefScanner.XrefScan(it).Any(jt => jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() ==
                        "Hip+Feet Tracking: 3 trackers found, tracking enabled."));
            var hipAndFeetTrackingSupported = (MethodInfo) XrefScanner.XrefScan(userOfHfts).Single(it =>
            {
                if (it.Type != XrefType.Method) return false;
                var resolved = it.TryResolve() as MethodInfo;
                return resolved != null && resolved.DeclaringType == typeof(VRCTrackingManager) && resolved.IsStatic &&
                       resolved.GetParameters().Length == 0 && resolved.ReturnType == typeof(bool);
            }).TryResolve();

            harmony.Patch(hipAndFeetTrackingSupported, new HarmonyMethod(typeof(FullBodyHandling), nameof(PatchHipAndFeetTracking)));
            ourIsFbtSupported = (Func<bool>) Delegate.CreateDelegate(typeof(Func<bool>), hipAndFeetTrackingSupported);
        }
    }
}