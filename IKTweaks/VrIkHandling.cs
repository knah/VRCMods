using System;
using System.Runtime.InteropServices;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;
using VRC.Core;

namespace IKTweaks
{
    public static class VrIkHandling
    {
        private static VRIK ourLastInitializedIk;
        private static IkController ourLastIkController;
        private static CachedSolver ourCachedSolver;
        private static AnimationsHandler ourAnimationsHandler;
        private static CustomSpineSolver ourCustomSpineSolver;
        private static WallFreezeFixer ourWallFreezeFixer;
        internal static HandOffsetsManager HandOffsetsManager;

        public static void HookVrIkInit()
        {
            var vrikInitMethod = typeof(VRCVrIkController).GetMethod(nameof(VRCVrIkController
                .Method_Public_Virtual_Final_New_Boolean_VRC_AnimationController_Animator_VRCPlayer_Boolean_0))!;

            NativePatchUtils.NativePatch(vrikInitMethod, out ourOriginalVrIkInit, VrIkInitReplacement);
            
            NativePatchUtils.NativePatch(typeof(IKSolverVR).GetMethod(nameof(IKSolverVR.VrcLateSolve))!, out ourOriginalSolverVrLateUpdate, SolverVrLateUpdatePatch);
            NativePatchUtils.NativePatch(typeof(IKSolver).GetMethod(nameof(IKSolver.Update))!, out ourOriginalSolverUpdate, SolverUpdatePatch);
            
            NativePatchUtils.NativePatch(typeof(IKSolverVR.Spine).GetMethod(nameof(IKSolverVR.Spine.SolvePelvis))!, out ourOriginalSolvePelvis, SolvePelvisPatch);
            NativePatchUtils.NativePatch(typeof(IKSolverVR.Spine).GetMethod(nameof(IKSolverVR.Spine.Solve))!, out ourOriginalSolveSpine, SolveSpinePatch);
            
            NativePatchUtils.NativePatch(typeof(IKSolverVR.Arm).GetMethod(nameof(IKSolverVR.Arm.VrcAvoidElbowClipping))!, out ourOriginalElbowClipping, ElbowClippingPatch);
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte VrIkInit(IntPtr a, IntPtr b, IntPtr c, IntPtr d, byte e, IntPtr n);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidDelegate(IntPtr thisPtr, IntPtr methodPtr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void BoolDelegate(IntPtr thisPtr, byte boolValue, IntPtr methodPtr);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SolveSpineDelegate(IntPtr thisPtr, IntPtr rootBonePtr, IntPtr legsPtr, IntPtr armsPtr, IntPtr methodPtr);

        private static VrIkInit ourOriginalVrIkInit;
        private static VoidDelegate ourOriginalSolverVrLateUpdate;
        private static VoidDelegate ourOriginalSolverUpdate;
        private static BoolDelegate ourOriginalElbowClipping;

        private static VoidDelegate ourOriginalSolvePelvis;
        private static SolveSpineDelegate ourOriginalSolveSpine;

        private static void SolverUpdatePatch(IntPtr thisPtr, IntPtr methodPtr) => SolverUpdatePatchImpl(thisPtr, methodPtr, ourOriginalSolverUpdate);
        private static void SolverVrLateUpdatePatch(IntPtr thisPtr, IntPtr methodPtr) => SolverUpdatePatchImpl(thisPtr, methodPtr, ourOriginalSolverVrLateUpdate);

        private static void SolverUpdatePatchImpl(IntPtr thisPtr, IntPtr methodPtr, VoidDelegate originalDelegate)
        {
            try
            {
                SolverUpdatePatchImplBody(thisPtr, methodPtr, originalDelegate);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception in IK solver: {ex}");
            }
        }

        private static void SolverUpdatePatchImplBody(IntPtr thisPtr, IntPtr methodPtr, VoidDelegate originalDelegate)
        {
            // ReSharper disable once Unity.NoNullPropagation
            if (ourCachedSolver.Solver?.Pointer != thisPtr)
            {
                originalDelegate(thisPtr, methodPtr);
                return;
            }

            using var weightCookie = new WeightAdjustmentCookie(ourCachedSolver);
            using var moveCookie = ourWallFreezeFixer.GetCookie();
            moveCookie.MoveTargets();
            ApplyVrIkSettings(in ourCachedSolver);
            
            if (ourCachedSolver.Spine.vrcFbtSpineMode)
            {
                var hasLegTargets = ourLastIkController.field_Private_IkType_0 == IkController.IkType.SixPoint;
                weightCookie.EnforceIkWeights(hasLegTargets);
                ourAnimationsHandler.ResetBonePositions(false, hasLegTargets);
            }
            
            originalDelegate(thisPtr, methodPtr);
        }

        private static Vector3 ourStoredPelvisPos;
        private static Quaternion ourStoredPelvisRot;
        private static Vector3 ourStoredHeadPos;
        private static Quaternion ourStoredHeadRot;

        private static void SolvePelvisPatch(IntPtr thisPtr, IntPtr methodPtr)
        {
            if (!IkTweaksSettings.FullBodyVrIk.Value || ourCachedSolver.Spine?.Pointer != thisPtr || !ourCachedSolver.Spine.vrcFbtSpineMode)
            {
                ourOriginalSolvePelvis(thisPtr, methodPtr);
                return;
            }
            
            // single lock modes modify these for some reason
            if (ourCachedSolver.Spine.vrcAllowHipDrift)
            {
                ourCachedSolver.Spine.IKPositionPelvis = ourStoredPelvisPos;
                ourCachedSolver.Spine.IKRotationPelvis = ourStoredPelvisRot;
            } else if (ourCachedSolver.Spine.vrcAllowHeadDrift)
            {
                ourCachedSolver.Spine.IKPositionHead = ourStoredHeadPos;
                ourCachedSolver.Spine.IKRotationHead = ourStoredHeadRot;
            }

            try
            {
                ourCustomSpineSolver.SolvePelvis();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception in IK spine solver: {ex}");
            }
        }

        private static void SolveSpinePatch(IntPtr thisPtr, IntPtr rootBonePtr, IntPtr legsPtr, IntPtr armsPtr, IntPtr methodPtr)
        {
            if (!IkTweaksSettings.FullBodyVrIk.Value || ourCachedSolver.Spine?.Pointer != thisPtr || !ourCachedSolver.Spine.vrcFbtSpineMode)
            {
                ourOriginalSolveSpine(thisPtr, rootBonePtr, legsPtr, armsPtr, methodPtr);
                return;
            }

            ourStoredPelvisPos = ourCachedSolver.Spine.IKPositionPelvis;
            ourStoredPelvisRot = ourCachedSolver.Spine.IKRotationPelvis;
            ourStoredHeadPos = ourCachedSolver.Spine.IKPositionHead;
            ourStoredHeadRot = ourCachedSolver.Spine.IKRotationHead;
            
            ourOriginalSolveSpine(thisPtr, rootBonePtr, legsPtr, armsPtr, methodPtr);
        }

        private static void ElbowClippingPatch(IntPtr thisPtr, byte boolValue, IntPtr methodPtr)
        {
            if (thisPtr != ourCachedSolver.LeftArm?.Pointer && thisPtr != ourCachedSolver.RightArm?.Pointer || !IkTweaksSettings.DisableElbowAvoidance.Value)
                ourOriginalElbowClipping(thisPtr, boolValue, methodPtr);
        }

        private static byte VrIkInitReplacement(IntPtr thisPtr, IntPtr vrcAnimController, IntPtr animatorPtr, IntPtr playerPtr, byte isLocalPlayer, IntPtr nativeMethod)
        {
            var __instance = new VRCVrIkController(thisPtr);
            var __2 = playerPtr == IntPtr.Zero ? null : new VRCPlayer(playerPtr);
            var animator = animatorPtr == IntPtr.Zero ? null : new Animator(animatorPtr);
            var result = ourOriginalVrIkInit(thisPtr, vrcAnimController, animatorPtr, playerPtr, isLocalPlayer, nativeMethod);
            VrikInitPatch(__instance, animator, __2);
            return result;
        }

        private static void VrikInitPatch(VRCVrIkController __instance, Animator animator, VRCPlayer? __2)
        {
            if (__2 != null && __2.prop_Player_0?.prop_APIUser_0?.id == APIUser.CurrentUser?.id)
            {
                ourLastIkController = __instance.field_Private_IkController_0;
                ourLastInitializedIk = __instance.field_Private_VRIK_0;
                ourCachedSolver = new CachedSolver(ourLastInitializedIk.solver);
                var handler = new HumanPoseHandler(animator.avatar, animator.transform);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                ourAnimationsHandler = new AnimationsHandler(handler, hips, in ourCachedSolver);
                ourCustomSpineSolver = new CustomSpineSolver(in ourCachedSolver);
                ourWallFreezeFixer = new WallFreezeFixer(__instance);
                HandOffsetsManager?.Dispose();
                HandOffsetsManager = new HandOffsetsManager(__instance);
            }
        }

        private static void ApplyVrIkSettings(in CachedSolver ikSolverVr)
        {
            var shoulderMode = IkTweaksSettings.ShoulderMode;
            ikSolverVr.LeftArm.shoulderRotationMode = shoulderMode;
            ikSolverVr.RightArm.shoulderRotationMode = shoulderMode;
            ikSolverVr.Spine.headClampWeight = IkTweaksSettings.Unrestrict3PointHeadRotation.Value ? 0 : 0.6f;
            // This is meaningless in FBT
            ikSolverVr.Solver.plantFeet = IkTweaksSettings.PlantFeet.Value && !ikSolverVr.Spine.vrcFbtSpineMode;
        }
    }
}