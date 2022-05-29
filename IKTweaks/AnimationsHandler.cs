using System;
using System.Linq;
using UnhollowerBaseLib;
using UnityEngine;

namespace IKTweaks
{
    internal class AnimationsHandler
    {
        private readonly Il2CppStructArray<float> myMuscles = new(HumanTrait.MuscleCount);
        private readonly HumanPoseHandler myPoseHandler;
        private readonly Transform myHips;
        private readonly CachedSolver mySolver;

        public AnimationsHandler(HumanPoseHandler poseHandler, Transform hips, in CachedSolver solver)
        {
            myPoseHandler = poseHandler;
            myHips = hips;
            mySolver = solver;
        }

        internal void ResetBonePositions(bool onlySpine, bool hasLegTargets)
        {
            if (mySolver.Solver.IKPositionWeight < 0.9f) return;

            myHips.get_position_Injected(out var hipPos);
            myHips.get_rotation_Injected(out var hipRot);

            myPoseHandler.GetHumanPose(out var bodyPos, out var bodyRot, myMuscles);

            for (var i = 0; i < myMuscles.Count; i++)
            {
                var currentMask = ourBoneResetMasks[i];
                if (onlySpine && currentMask != BoneResetMask.Spine) continue;
                if (!hasLegTargets && (currentMask == BoneResetMask.LeftLeg || currentMask == BoneResetMask.RightLeg)) continue;
                
                if (IkTweaksSettings.IgnoreAnimationsModeParsed == IgnoreAnimationsMode.All)
                {
                    myMuscles[i] *= currentMask == BoneResetMask.Never ? 1 : 0;
                    continue;
                }

                switch (currentMask)
                {
                    case BoneResetMask.Never:
                        break;
                    case BoneResetMask.Spine:
                        myMuscles[i] *= 1 - mySolver.Spine.pelvisPositionWeight;
                        break;
                    case BoneResetMask.LeftArm:
                        myMuscles[i] *= 1 - mySolver.LeftArm.positionWeight;
                        break;
                    case BoneResetMask.RightArm:
                        myMuscles[i] *= 1 - mySolver.RightArm.positionWeight;
                        break;
                    case BoneResetMask.LeftLeg:
                        myMuscles[i] *= 1 - mySolver.LeftLeg.positionWeight;
                        break;
                    case BoneResetMask.RightLeg:
                        myMuscles[i] *= 1 - mySolver.RightLeg.positionWeight;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            myPoseHandler.SetHumanPose(ref bodyPos, ref bodyRot, myMuscles);

            myHips.position = hipPos;
            myHips.rotation = hipRot;
        }


        private enum BoneResetMask
        {
            Never,
            Spine,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg,
        }

        private static readonly string[] ourNeverBones = { "Index", "Thumb", "Middle", "Ring", "Little", "Jaw", "Eye" };
        private static readonly string[] ourArmBones = { "Arm", "Forearm", "Hand", "Shoulder" };
        private static readonly string[] ourLegBones = { "Leg", "Foot", "Toes" };

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

        private static readonly BoneResetMask[] ourBoneResetMasks = HumanTrait.MuscleName.Select(JudgeBone).ToArray();
    }
}