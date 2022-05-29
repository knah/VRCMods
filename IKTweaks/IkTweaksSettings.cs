using System;
using System.ComponentModel;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;

namespace IKTweaks
{
    public static class IkTweaksSettings
    {
        internal const string IkTweaksCategory = "IkTweaks";

        internal static Vector3 DefaultHandAngle = new(0, 10, 0);
        internal static Vector3 DefaultHandOffset = new(0, 0, 0);
        

        internal static void RegisterSettings()
        {
            var category = Category = MelonPreferences.CreateCategory(IkTweaksCategory, "IK Tweaks");
            
            FixShoulders = category.CreateEntry("PitchYawShoulders", true, "Use Pitch-Yaw Shoulders");
            IgnoreAnimationsMode = category.CreateEntry(nameof(IgnoreAnimationsMode), IKTweaks.IgnoreAnimationsMode.HandAndHead, "Animations mode in FBT");
            PlantFeet = category.CreateEntry(nameof(PlantFeet), false, "Feet stick to ground");
            
            FullBodyVrIk = category.CreateEntry(nameof(FullBodyVrIk), true, "Use IKTweaks spine solver");
            PinHipRotation = category.CreateEntry(nameof(PinHipRotation), true, "Enforce hip rotation match");
            DoHipShifting = category.CreateEntry(nameof(DoHipShifting), true, "Shift hip pivot (support inverted hip)");
            
            PreStraightenSpine = category.CreateEntry(nameof(PreStraightenSpine), false, "Pre-straighten spine (improve IK stability)");
            StraightenNeck = category.CreateEntry(nameof(StraightenNeck), true, "Straighten neck");
            SpineRelaxIterations = category.CreateEntry(nameof(SpineRelaxIterations), 10, "Spine Relax Iterations (max 25)");

            MaxSpineAngleFwd = category.CreateEntry(nameof(MaxSpineAngleFwd), 30f, "Max spine angle forward (degrees)");
            MaxSpineAngleBack = category.CreateEntry(nameof(MaxSpineAngleBack), 30f, "Max spine angle back (degrees)");
            MaxNeckAngleFwd = category.CreateEntry(nameof(MaxNeckAngleFwd), 30f, "Max neck angle forward (degrees)");
            
            MaxNeckAngleBack = category.CreateEntry(nameof(MaxNeckAngleBack), 15f, "Max neck angle back (degrees)");
            NeckPriority = category.CreateEntry(nameof(NeckPriority), 2f, "Neck bend priority (1=even with spine, 2=twice as much as spine)");
            StraightSpineAngle = category.CreateEntry(nameof(StraightSpineAngle), 15f, "Straight spine angle (degrees)");

            StraightSpinePower = category.CreateEntry(nameof(StraightSpinePower), 2f, "Straight spine power");
            Unrestrict3PointHeadRotation = category.CreateEntry(nameof(Unrestrict3PointHeadRotation), true, "Allow more head rotation");
            NoWallFreeze = category.CreateEntry(nameof(NoWallFreeze), true, "Don't freeze head/hands inside walls");

            DisableElbowAvoidance = category.CreateEntry(nameof(DisableElbowAvoidance), false, "Disable IK2 elbow-chest avoidance");

            ExperimentalSettingOne = category.CreateEntry(nameof(ExperimentalSettingOne), false, "Experimental setting", dont_save_default: true, is_hidden: true);

            HandAngleOffset = category.CreateEntry(nameof(HandAngleOffset) + "2", DefaultHandAngle, "Hand angle offset", null, true);
            HandPositionOffset = category.CreateEntry(nameof(HandPositionOffset) + "2", DefaultHandOffset, "Hand position offset", null, true);
        }

        public static IKSolverVR.Arm.ShoulderRotationMode ShoulderMode => FixShoulders.Value ? IKSolverVR.Arm.ShoulderRotationMode.YawPitch : IKSolverVR.Arm.ShoulderRotationMode.FromTo;

        internal static MelonPreferences_Category Category;
        
        public static MelonPreferences_Entry<bool> FixShoulders;
        public static MelonPreferences_Entry<IgnoreAnimationsMode> IgnoreAnimationsMode;
        public static MelonPreferences_Entry<bool> PlantFeet;
        public static MelonPreferences_Entry<bool> FullBodyVrIk;
        public static MelonPreferences_Entry<float> MaxSpineAngleFwd;
        public static MelonPreferences_Entry<float> MaxSpineAngleBack;
        public static MelonPreferences_Entry<int> SpineRelaxIterations;
        public static MelonPreferences_Entry<float> MaxNeckAngleFwd;
        public static MelonPreferences_Entry<float> MaxNeckAngleBack;
        public static MelonPreferences_Entry<float> NeckPriority;
        public static MelonPreferences_Entry<bool> StraightenNeck;
        public static MelonPreferences_Entry<float> StraightSpineAngle;
        public static MelonPreferences_Entry<float> StraightSpinePower;
        public static MelonPreferences_Entry<bool> PinHipRotation;
        public static MelonPreferences_Entry<bool> DoHipShifting;
        public static MelonPreferences_Entry<bool> PreStraightenSpine;
        public static MelonPreferences_Entry<bool> Unrestrict3PointHeadRotation;
        public static MelonPreferences_Entry<bool> NoWallFreeze;
        public static MelonPreferences_Entry<bool> DisableElbowAvoidance;
        public static MelonPreferences_Entry<bool> ExperimentalSettingOne;
        
        public static MelonPreferences_Entry<Vector3> HandAngleOffset;
        public static MelonPreferences_Entry<Vector3> HandPositionOffset;

        public static IgnoreAnimationsMode IgnoreAnimationsModeParsed => IgnoreAnimationsMode.Value;
    }

    [Flags]
    public enum IgnoreAnimationsMode
    {
        [Description("Play all animations")]
        None = 0,
        [Description("Ignore head animations")]
        Head = 1,
        [Description("Ignore hands animations")]
        Hands = 2,
        [Description("Ignore head and hands")]
        HandAndHead = Head | Hands,
        [Description("Ignore others (hips/feet)")]
        Others = 4,
        [Description("Ignore all (always slide around)")]
        All = HandAndHead | Others
    }
}