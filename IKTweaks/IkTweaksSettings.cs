using System;
using MelonLoader;
using RootMotion.FinalIK;
using UnityEngine;

namespace IKTweaks
{
    public static class IkTweaksSettings
    {
        internal const string IkTweaksCategory = "IkTweaks";

        internal static Vector3 DefaultHandAngle = new(0, -105, 0);
        internal static Vector3 DefaultHandOffset = new(0.015f, -0.005f, 0);
        

        internal static void RegisterSettings()
        {
            var category = Category = MelonPreferences.CreateCategory(IkTweaksCategory, "IK Tweaks");
            
            FixShoulders = category.CreateEntry("PitchYawShoulders", true, "Use Pitch-Yaw Shoulders");
            IgnoreAnimationsMode = category.CreateEntry(nameof(IgnoreAnimationsMode), nameof(IKTweaks.IgnoreAnimationsMode.HandAndHead), "Animations mode in FBT");
            PlantFeet = category.CreateEntry(nameof(PlantFeet), false, "Feet stick to ground");
            
            FullBodyVrIk = category.CreateEntry(nameof(FullBodyVrIk), true, "Enable IKTweaks (use custom VRIK)");
            AddHumanoidPass = category.CreateEntry(nameof(AddHumanoidPass), true, "Enforce local NetIK (see what others see)");
            MapToes = category.CreateEntry(nameof(MapToes), false, "Map toes (use if your feet trackers move with your toes)");

            UseKneeTrackers = category.CreateEntry(nameof(UseKneeTrackers), false, "Use knee trackers");
            UseElbowTrackers = category.CreateEntry(nameof(UseElbowTrackers), false, "Use elbow trackers");
            UseChestTracker = category.CreateEntry(nameof(UseChestTracker), false, "Use chest tracker");
            
            CalibrateFollowHead = category.CreateEntry(nameof(CalibrateFollowHead), true, "Avatar follows head when calibrating (recommended)");
            CalibrateHalfFreeze = category.CreateEntry(nameof(CalibrateHalfFreeze), true, "Freeze avatar on one trigger hold in follow head mode");
            CalibrateUseUniversal = category.CreateEntry(nameof(CalibrateUseUniversal), true, "Use universal calibration (requires follow head mode)");

            CalibrateStorePerAvatar = category.CreateEntry(nameof(CalibrateStorePerAvatar), true, "Store calibration per avatar (when not using universal calibration)");
            APoseCalibration = category.CreateEntry(nameof(APoseCalibration), false, "A-pose calibration");
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
            MeasureMode = category.CreateEntry(nameof(MeasureMode), nameof(MeasureAvatarMode.ImprovedWingspan), "Avatar scaling mode");
            
            Unrestrict3PointHeadRotation = category.CreateEntry(nameof(Unrestrict3PointHeadRotation), true, "Allow more head rotation in 3/4-point tracking");
            WingspanMeasurementAdjustFactor = category.CreateEntry(nameof(WingspanMeasurementAdjustFactor), 1.1f, "Improved wingspan adjustment factor");
            OneHandedCalibration = category.CreateEntry(nameof(OneHandedCalibration), false, "One-handed calibration");
            
            ElbowGoalOffset = category.CreateEntry(nameof(ElbowGoalOffset), 0.1f, "Elbows bend goal offset (0-1)");
            KneeGoalOffset = category.CreateEntry(nameof(KneeGoalOffset), 0.1f, "Knees bend goal offset (0-1)");
            ChestGoalOffset = category.CreateEntry(nameof(ChestGoalOffset), 0.5f, "Chest bend goal offset (0-1)");
            
            NoWallFreeze = category.CreateEntry(nameof(NoWallFreeze), true, "Don't freeze head/hands inside walls");

            ExperimentalSettingOne = category.CreateEntry(nameof(ExperimentalSettingOne), false, "Experimental setting", dont_save_default: true, is_hidden: true);
            
            HandAngleOffset = category.CreateEntry(nameof(HandAngleOffset), DefaultHandAngle, "Hand angle offset", null, true);
            HandPositionOffset = category.CreateEntry(nameof(HandPositionOffset), DefaultHandOffset, "Hand position offset", null, true);

            IgnoreAnimationsMode.OnValueChanged += (_, v) => UpdateIgnoreAnimationMode(v);
            UpdateIgnoreAnimationMode(IgnoreAnimationsMode.Value);

            MeasureMode.OnValueChanged += (_, v) => UpdateMeasureMode(v);
            UpdateMeasureMode(MeasureMode.Value);
        }

        private static void UpdateMeasureMode(string value)
        {
            if (Enum.TryParse(value, true, out MeasureAvatarMode mode)) 
                MeasureModeParsed = mode;
        }

        private static void UpdateIgnoreAnimationMode(string value)
        {
            if (Enum.TryParse(value, true, out IgnoreAnimationsMode mode)) 
                IgnoreAnimationsModeParsed = mode;
        }

        public static IKSolverVR.Arm.ShoulderRotationMode ShoulderMode => FixShoulders.Value ? IKSolverVR.Arm.ShoulderRotationMode.YawPitch : IKSolverVR.Arm.ShoulderRotationMode.FromTo;

        internal static MelonPreferences_Category Category;
        
        public static MelonPreferences_Entry<bool> FixShoulders;
        public static MelonPreferences_Entry<bool> CalibrateHalfFreeze;
        public static MelonPreferences_Entry<bool> CalibrateFollowHead;
        public static MelonPreferences_Entry<bool> CalibrateUseUniversal;
        public static MelonPreferences_Entry<bool> CalibrateStorePerAvatar;
        public static MelonPreferences_Entry<bool> UseKneeTrackers;
        public static MelonPreferences_Entry<bool> UseElbowTrackers;
        public static MelonPreferences_Entry<bool> UseChestTracker;
        public static MelonPreferences_Entry<string> IgnoreAnimationsMode;
        public static MelonPreferences_Entry<bool> PlantFeet;
        public static MelonPreferences_Entry<bool> FullBodyVrIk;
        public static MelonPreferences_Entry<float> MaxSpineAngleFwd;
        public static MelonPreferences_Entry<float> MaxSpineAngleBack;
        public static MelonPreferences_Entry<int> SpineRelaxIterations;
        public static MelonPreferences_Entry<float> MaxNeckAngleFwd;
        public static MelonPreferences_Entry<float> MaxNeckAngleBack;
        public static MelonPreferences_Entry<float> NeckPriority;
        public static MelonPreferences_Entry<bool> AddHumanoidPass;
        public static MelonPreferences_Entry<bool> MapToes;
        public static MelonPreferences_Entry<bool> StraightenNeck;
        public static MelonPreferences_Entry<float> StraightSpineAngle;
        public static MelonPreferences_Entry<float> StraightSpinePower;
        public static MelonPreferences_Entry<bool> PinHipRotation;
        public static MelonPreferences_Entry<bool> DoHipShifting;
        public static MelonPreferences_Entry<bool> PreStraightenSpine;
        public static MelonPreferences_Entry<string> MeasureMode;
        public static MelonPreferences_Entry<bool> APoseCalibration;
        public static MelonPreferences_Entry<bool> Unrestrict3PointHeadRotation;
        public static MelonPreferences_Entry<float> WingspanMeasurementAdjustFactor;
        public static MelonPreferences_Entry<bool> OneHandedCalibration;
        public static MelonPreferences_Entry<bool> NoWallFreeze;
        public static MelonPreferences_Entry<bool> ExperimentalSettingOne;
        
        public static MelonPreferences_Entry<float> ElbowGoalOffset;
        public static MelonPreferences_Entry<float> KneeGoalOffset;
        public static MelonPreferences_Entry<float> ChestGoalOffset;
        
        public static MelonPreferences_Entry<Vector3> HandAngleOffset;
        public static MelonPreferences_Entry<Vector3> HandPositionOffset;

        public static IgnoreAnimationsMode IgnoreAnimationsModeParsed;
        public static MeasureAvatarMode MeasureModeParsed;
    }

    [Flags]
    public enum IgnoreAnimationsMode
    {
        None = 0,
        Head = 1,
        Hands = 2,
        HandAndHead = Head | Hands,
        Others = 4,
        All = HandAndHead | Others
    }

    public enum MeasureAvatarMode
    {
        Default,
        Height,
        ImprovedWingspan
    }

    public enum DriftPreference
    {
        Hips,
        Viewpoint,
        Custom
    }
}