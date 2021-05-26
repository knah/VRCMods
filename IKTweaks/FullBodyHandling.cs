using System;
using System.Collections.Generic;
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
    public static class FullBodyHandling
    {
        private static T MinBy<T>(this IEnumerable<T> src, Func<T, int> selector)
        {
            var seenElement = false;
            var minValue = int.MaxValue;
            T best = default;
            foreach (var x in src)
            {
                seenElement = true;
                var current = selector(x);
                if (current < minValue)
                {
                    minValue = current;
                    best = x;
                }
            }

            if (!seenElement)
                throw new ArgumentException("Sequence is empty");

            return best;
        }
        
        internal static VRCFbbIkController LastInitializedController;
        internal static VRIK_New LastInitializedVRIK;

        internal static float LeftElbowWeight = 0f;
        internal static float RightElbowWeight = 0f;
        internal static float LeftKneeWeight = 0f;
        internal static float RightKneeWeight = 0f;
        internal static float ChestWeight = 0f;

        public static bool LastCalibrationWasInCustomIk;
        private static Func<bool> ourIsFbtSupported;
        
        public static void Update()
        {
            if (!IkTweaksSettings.FullBodyVrIk.Value || !LastCalibrationWasInCustomIk) return;

            if (LastInitializedController == null || LastInitializedController.field_Private_FullBodyBipedIK_0 == null) return;
            
            var fbbik = LastInitializedController.field_Private_FullBodyBipedIK_0;
            
            var vrik = fbbik.GetComponent<VRIK_New>();

            var firstPuckDisabled = !IKTweaksMod.ourRandomPuck.activeInHierarchy;
            
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
                if (!IkTweaksSettings.IgnoreAnimations.Value || firstPuckDisabled)
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

            if (!IkTweaksSettings.IgnoreAnimations.Value || firstPuckDisabled)
            {
                var leftLegMappingWeight = fbbik.solver.leftLegMapping.weight;
                vrik.solver.leftLeg.positionWeight = fbbik.solver.leftFootEffector.positionWeight * leftLegMappingWeight;
                vrik.solver.leftLeg.rotationWeight = fbbik.solver.leftFootEffector.rotationWeight * leftLegMappingWeight;

                var rightLegMappingWeight = fbbik.solver.rightLegMapping.weight;
                vrik.solver.rightLeg.positionWeight = fbbik.solver.rightFootEffector.positionWeight * rightLegMappingWeight;
                vrik.solver.rightLeg.rotationWeight = fbbik.solver.rightFootEffector.rotationWeight * rightLegMappingWeight;
                
                vrik.solver.spine.pelvisPositionWeight = fbbik.solver.bodyEffector.positionWeight;
                vrik.solver.spine.pelvisRotationWeight = fbbik.solver.bodyEffector.rotationWeight;

                var leftArmMappingWeight = fbbik.solver.leftArmMapping.weight;
                vrik.solver.leftArm.positionWeight = fbbik.solver.leftHandEffector.positionWeight * leftArmMappingWeight;
                vrik.solver.leftArm.rotationWeight = fbbik.solver.leftHandEffector.rotationWeight * leftArmMappingWeight;

                var rightArmMappingWeight = fbbik.solver.rightArmMapping.weight;
                vrik.solver.rightArm.positionWeight = fbbik.solver.rightHandEffector.positionWeight * rightArmMappingWeight;
                vrik.solver.rightArm.rotationWeight = fbbik.solver.rightHandEffector.rotationWeight * rightArmMappingWeight;
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
            
            vrik.solver.spine.maxNeckAngleFwd = IkTweaksSettings.MaxNeckAngleFwd.Value;
            vrik.solver.spine.maxNeckAngleBack = IkTweaksSettings.MaxNeckAngleBack.Value;
            vrik.solver.spine.maxSpineAngleFwd = IkTweaksSettings.MaxSpineAngleFwd.Value;
            vrik.solver.spine.maxSpineAngleBack = IkTweaksSettings.MaxSpineAngleBack.Value;
            vrik.solver.spine.relaxationIterations = IkTweaksSettings.SpineRelaxIterations.Value;
            if (vrik.solver.spine.relaxationIterations > 25) vrik.solver.spine.relaxationIterations = 25;
            if (vrik.solver.spine.relaxationIterations < 5) vrik.solver.spine.relaxationIterations = 5;
            // vrik.solver.spine.trigPelvisStrength = IkTweaksSettings.SpineBendHipsStrength;
            vrik.solver.spine.neckBendPriority = IkTweaksSettings.NeckPriority.Value;

            if (IkTweaksSettings.UseKneeTrackers.Value)
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
            
            vrik.solver.leftArm.bendGoalWeight = IkTweaksSettings.UseElbowTrackers.Value ? LeftElbowWeight : 0;
            vrik.solver.rightArm.bendGoalWeight = IkTweaksSettings.UseElbowTrackers.Value ? RightElbowWeight : 0;
            vrik.solver.spine.chestGoalWeight = IkTweaksSettings.UseChestTracker.Value ? ChestWeight : 0;

            vrik.solver.spine.hipRotationPinning = IkTweaksSettings.PinHipRotation.Value;
        }

        public enum BoneResetMask
        {
            Never,
            Spine,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg,
        }

        private static readonly string[] ourNeverBones = {"Index", "Thumb", "Middle", "Ring", "Little", "Jaw", "Eye"};
        private static readonly string[] ourArmBones = {"Arm", "Forearm", "Hand", "Shoulder"};
        private static readonly string[] ourLegBones = {"Leg", "Foot", "Toes"};

        private static BoneResetMask JudgeBone(string name)
        {
            if (ourNeverBones.Any(name.Contains))
                return BoneResetMask.Never;

            if (ourArmBones.Any(name.Contains))
            {
                return name.Contains("Left") ? BoneResetMask.LeftArm : BoneResetMask.RightArm;
            }

            if (ourLegBones.Any(name.Contains))
                return name.Contains("Left") ? BoneResetMask.LeftLeg : BoneResetMask.RightLeg;

            return BoneResetMask.Spine;
        }

        internal static readonly BoneResetMask[] ourBoneResetMasks = HumanTrait.MuscleName.Select(JudgeBone).ToArray(); 

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
                    if (vrik.solver.IKPositionWeight < 0.9f) return;
                    
                    var hipPos = hips.position;
                    var hipRot = hips.rotation;
                    
                    handler.GetHumanPose(out var bodyPos, out var bodyRot, muscles);
                    
                    for (var i = 0; i < muscles.Count; i++)
                    {
                        if (IkTweaksSettings.IgnoreAnimations.Value && IKTweaksMod.ourRandomPuck.activeInHierarchy)
                        {
                            muscles[i] *= ourBoneResetMasks[i] == BoneResetMask.Never ? 1 : 0;
                            continue;
                        }
                        
                        switch (ourBoneResetMasks[i])
                        {
                            case BoneResetMask.Never:
                                break;
                            case BoneResetMask.Spine:
                                muscles[i] *= 1 - vrik.solver.spine.pelvisPositionWeight;
                                break;
                            case BoneResetMask.LeftArm:
                                muscles[i] *= 1 - vrik.solver.leftArm.positionWeight;
                                break;
                            case BoneResetMask.RightArm:
                                muscles[i] *= 1 - vrik.solver.rightArm.positionWeight;
                                break;
                            case BoneResetMask.LeftLeg:
                                muscles[i] *= 1 - vrik.solver.leftLeg.positionWeight;
                                break;
                            case BoneResetMask.RightLeg:
                                muscles[i] *= 1 - vrik.solver.rightLeg.positionWeight;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                            
                    }

                    handler.SetHumanPose(ref bodyPos, ref bodyRot, muscles);

                    hips.position = hipPos;
                    hips.rotation = hipRot;
                };

                vrik.solver.OnPostUpdate += () =>
                {
                    if (!IkTweaksSettings.AddHumanoidPass.Value) return;
                    
                    var hipPos = hips.position;
                    var hipRot = hips.rotation;

                    handler.GetHumanPose(out var bodyPos, out var bodyRot, muscles);

                    handler.SetHumanPose(ref bodyPos, ref bodyRot, muscles);

                    hips.position = hipPos;
                    hips.rotation = hipRot;
                };
            }

            LastInitializedVRIK = vrik;

            vrik.enabled = false;
        }

        public static VRIK_New SetupVrIk(VRCFbbIkController source, GameObject targetGameObject)
        {
            var vrik = targetGameObject.GetComponent<VRIK_New>();
            
            vrik.AutoDetectReferences();
            
            if (!IkTweaksSettings.MapToes.Value)
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

            vrik.solver.spine.headTarget = source.field_Private_FBBIKHeadEffector_0.transform;

            return vrik;
        }

        private static void FbbIkInitPrefix(VRCFbbIkController __instance, VRCPlayer? __2, bool __3)
        {
            var vrcPlayer = __2;
            if (vrcPlayer == null) return;
            var isLocalPlayer = vrcPlayer.prop_Player_0?.prop_APIUser_0?.id == APIUser.CurrentUser?.id;
            if(isLocalPlayer != __3) MelonLogger.Warning("Computed IsLocal is different from provided");
            if (!isLocalPlayer) return;
            
            LastCalibrationWasInCustomIk = false;
            LastInitializedController = __instance;
        }

        private static void FbbIkInitPostfix(Animator __1, bool __3)
        {
            if (!__3 || !IkTweaksSettings.FullBodyVrIk.Value || !ourIsFbtSupported()) return;
            
            LastCalibrationWasInCustomIk = true;
            CalibrationManager.Calibrate(__1.gameObject);
        }

        private static bool PatchHipAndFeetTracking(ref bool __result)
        {
            if (IkTweaksSettings.DisableFbt.Value)
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static bool LateUpdatePrefix(FullBodyBipedIK __instance)
        {
            IKTweaksMod.ProcessIKLateUpdateQueue();

            if (IkTweaksSettings.FullBodyVrIk.Value && LastCalibrationWasInCustomIk &&
                LastInitializedController.field_Private_FullBodyBipedIK_0 == __instance)
            {
                Update();
                if(LastInitializedVRIK != null)
                    LastInitializedVRIK.LateUpdate_ManualDrive();
                return false;
            }

            return true;
        }
        
        private static bool FixedUpdatePrefix(FullBodyBipedIK __instance)
        {
            if (IkTweaksSettings.FullBodyVrIk.Value && LastCalibrationWasInCustomIk &&
                LastInitializedController.field_Private_FullBodyBipedIK_0 == __instance)
            {
                if(LastInitializedVRIK != null)
                    LastInitializedVRIK.FixedUpdate_ManualDrive();
                return false;
            }

            return true;
        }
        
        private static bool UpdatePrefix(FullBodyBipedIK __instance)
        {
            if (IkTweaksSettings.FullBodyVrIk.Value && LastCalibrationWasInCustomIk &&
                LastInitializedController.field_Private_FullBodyBipedIK_0 == __instance)
            {
                if(LastInitializedVRIK != null)
                    LastInitializedVRIK.Update_ManualDrive();
                return false;
            }

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
            if (IkTweaksSettings.FullBodyVrIk.Value && ourIsFbtSupported())
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
                new HarmonyMethod(typeof(FullBodyHandling), nameof(UpdatePrefix)));
            harmony.Patch(AccessTools.Method(typeof(FullBodyBipedIK), nameof(FullBodyBipedIK.FixedUpdate)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(FixedUpdatePrefix)));

            harmony.Patch(AccessTools.Method(typeof(VRCTrackingManager), nameof(VRCTrackingManager.Method_Public_Static_Boolean_String_0)),
                new HarmonyMethod(typeof(FullBodyHandling), nameof(IsCalibratedForAvatarPrefix)));
            
            var userOfHfts = typeof(VRCFbbIkController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(it =>
                    XrefScanner.XrefScan(it).Any(jt => jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() ==
                        "Hip+Feet Tracking: 3 trackers found, tracking enabled.")).MinBy(it => XrefScanner.XrefScan(it).Count());
            var hipAndFeetTrackingSupportedCandidates = XrefScanner.XrefScan(userOfHfts).Where(it =>
            {
                if (it.Type != XrefType.Method) return false;
                var resolved = it.TryResolve() as MethodInfo;
                return resolved != null && resolved.DeclaringType == typeof(VRCTrackingManager) && resolved.IsStatic &&
                       resolved.GetParameters().Length == 0 && resolved.ReturnType == typeof(bool);
            }).ToList();
            
            foreach (var hipAndFeetTrackingSupportedCandidate in hipAndFeetTrackingSupportedCandidates)
                MelonDebug.Msg("hafts candidate: " + hipAndFeetTrackingSupportedCandidate.TryResolve()?.FullDescription());

            var hipAndFeetTrackingSupported = (MethodInfo) hipAndFeetTrackingSupportedCandidates.First().TryResolve();
            harmony.Patch(hipAndFeetTrackingSupported, new HarmonyMethod(typeof(FullBodyHandling), nameof(PatchHipAndFeetTracking)));
            ourIsFbtSupported = (Func<bool>) Delegate.CreateDelegate(typeof(Func<bool>), hipAndFeetTrackingSupported);
        }
    }
}