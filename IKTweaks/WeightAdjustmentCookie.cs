using System;
using System.Runtime.CompilerServices;

namespace IKTweaks
{
    public struct WeightAdjustmentCookie: IDisposable
    {
        private readonly CachedSolver mySolver;

        private readonly (float PositionWeight, float RotationWeight) myStoredHeadWeight;
        private readonly (float PositionWeight, float RotationWeight) myStoredPelvisWeight;
        private readonly float myStoredChestWeight;
        private readonly (float PositionWeight, float RotationWeight) myStoredLeftArmWeight;
        private readonly (float PositionWeight, float RotationWeight) myStoredRightArmWeight;
        private readonly (float PositionWeight, float RotationWeight) myStoredLeftLegWeight;
        private readonly (float PositionWeight, float RotationWeight) myStoredRightLegWeight;
        private readonly float myStoredLeftKneeWeight;
        private readonly float myStoredRightKneeWeight;

        private readonly (bool animatedNormal, bool kneeTarget) myStoredLeftKneeBend;
        private readonly (bool animatedNormal, bool kneeTarget) myStoredRightKneeBend;

        private bool myChangedWeights;
        
        public WeightAdjustmentCookie(CachedSolver solver)
        {
            mySolver = solver;

            myStoredHeadWeight = (solver.Spine.positionWeight, solver.Spine.rotationWeight);
            myStoredPelvisWeight = (solver.Spine.pelvisPositionWeight, solver.Spine.pelvisRotationWeight);
            myStoredChestWeight = solver.Spine.chestGoalWeight;
            myStoredLeftArmWeight = (solver.LeftArm.positionWeight, solver.LeftArm.rotationWeight);
            myStoredRightArmWeight = (solver.RightArm.positionWeight, solver.RightArm.rotationWeight);
            myStoredLeftLegWeight = (solver.LeftLeg.positionWeight, solver.LeftLeg.rotationWeight);
            myStoredRightLegWeight = (solver.RightLeg.positionWeight, solver.RightLeg.rotationWeight);
            myStoredLeftKneeWeight = solver.LeftLeg.bendGoalWeight;
            myStoredRightKneeWeight = solver.RightLeg.bendGoalWeight;

            myStoredLeftKneeBend = (solver.LeftLeg.useAnimatedBendNormal, solver.LeftLeg.vrcUseKneeTarget);
            myStoredRightKneeBend = (solver.RightLeg.useAnimatedBendNormal, solver.RightLeg.vrcUseKneeTarget);

            // elbow weights?
            
            myChangedWeights = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetHandWeight(float inWeight)
        {
            if ((IkTweaksSettings.IgnoreAnimationsModeParsed & IgnoreAnimationsMode.Hands) != 0)
                return 1;
            
            return inWeight;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetHeadWeight(float inWeight)
        {
            if ((IkTweaksSettings.IgnoreAnimationsModeParsed & IgnoreAnimationsMode.Head) != 0)
                return 1;
            
            return inWeight;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetOtherWeight(float inWeight)
        {
            if ((IkTweaksSettings.IgnoreAnimationsModeParsed & IgnoreAnimationsMode.Others) != 0)
                return 1;
            
            return inWeight;
        }

        public void EnforceIkWeights(bool hasLegTargets)
        {
            var ignoreMode = IkTweaksSettings.IgnoreAnimationsModeParsed;
            if (ignoreMode == IgnoreAnimationsMode.None) return;

            myChangedWeights = true;
            
            if (ignoreMode != IgnoreAnimationsMode.All)
            {
                if (hasLegTargets)
                {
                    mySolver.LeftLeg.positionWeight = GetOtherWeight(mySolver.LeftLeg.positionWeight);
                    mySolver.LeftLeg.rotationWeight = GetOtherWeight(mySolver.LeftLeg.rotationWeight);

                    mySolver.RightLeg.positionWeight = GetOtherWeight(mySolver.RightLeg.positionWeight);
                    mySolver.RightLeg.rotationWeight = GetOtherWeight(mySolver.RightLeg.rotationWeight);
                }

                mySolver.Spine.pelvisPositionWeight = GetOtherWeight(mySolver.Spine.pelvisPositionWeight);
                mySolver.Spine.pelvisRotationWeight = GetOtherWeight(mySolver.Spine.pelvisRotationWeight);
                
                mySolver.Spine.positionWeight = GetHeadWeight(mySolver.Spine.positionWeight);
                mySolver.Spine.rotationWeight = GetHeadWeight(mySolver.Spine.rotationWeight);
                
                mySolver.LeftArm.positionWeight = GetHandWeight(mySolver.LeftArm.positionWeight);
                mySolver.LeftArm.rotationWeight = GetHandWeight(mySolver.LeftArm.rotationWeight);
                
                mySolver.RightArm.positionWeight = GetHandWeight(mySolver.RightArm.positionWeight);
                mySolver.RightArm.rotationWeight = GetHandWeight(mySolver.RightArm.rotationWeight);
            }
            else
            {
                if (hasLegTargets)
                {
                    mySolver.LeftLeg.positionWeight = 1;
                    mySolver.LeftLeg.rotationWeight = 1;

                    mySolver.RightLeg.positionWeight = 1;
                    mySolver.RightLeg.rotationWeight = 1;
                }

                mySolver.Spine.pelvisPositionWeight = 1;
                mySolver.Spine.pelvisRotationWeight = 1;
                
                mySolver.Spine.positionWeight = 1;
                mySolver.Spine.rotationWeight = 1;

                mySolver.LeftArm.positionWeight = 1;
                mySolver.LeftArm.rotationWeight = 1;

                mySolver.RightArm.positionWeight = 1;
                mySolver.RightArm.rotationWeight = 1;
            }
            
            if (hasLegTargets && (ignoreMode & IgnoreAnimationsMode.Others) != 0)
            {
                mySolver.LeftLeg.useAnimatedBendNormal = false;
                mySolver.RightLeg.useAnimatedBendNormal = false;
                        
                mySolver.LeftLeg.vrcUseKneeTarget = true;
                mySolver.RightLeg.vrcUseKneeTarget = true;
            }
        }

        public void Dispose()
        {
            if (!myChangedWeights) return;
            
            var solver = mySolver;
            
            (solver.Spine.positionWeight, solver.Spine.rotationWeight) = myStoredHeadWeight;
            (solver.Spine.pelvisPositionWeight, solver.Spine.pelvisRotationWeight) = myStoredPelvisWeight;
            solver.Spine.chestGoalWeight = myStoredChestWeight;
            (solver.LeftArm.positionWeight, solver.LeftArm.rotationWeight) = myStoredLeftArmWeight;
            (solver.RightArm.positionWeight, solver.RightArm.rotationWeight) = myStoredRightArmWeight;
            (solver.LeftLeg.positionWeight, solver.LeftLeg.rotationWeight) = myStoredLeftLegWeight;
            (solver.RightLeg.positionWeight, solver.RightLeg.rotationWeight) = myStoredRightLegWeight;
            solver.LeftLeg.bendGoalWeight = myStoredLeftKneeWeight;
            solver.RightLeg.bendGoalWeight = myStoredRightKneeWeight;
            
            (solver.LeftLeg.useAnimatedBendNormal, solver.LeftLeg.vrcUseKneeTarget) = myStoredLeftKneeBend;
            (solver.RightLeg.useAnimatedBendNormal, solver.RightLeg.vrcUseKneeTarget) = myStoredRightKneeBend;
            
            // elbow weights?
        }
    }
}