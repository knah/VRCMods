using MelonLoader;
using RootMotion.FinalIK;

namespace IKTweaks
{
    public static class IkTweaksSettings
    {
        private const string IkTweaksCategory = "IkTweaks";

        internal static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory(IkTweaksCategory, "IK Tweaks");
            
            FixShoulders = (MelonPreferences_Entry<bool>) category.CreateEntry("PitchYawShoulders", true, "Use Pitch-Yaw Shoulders");
            IgnoreAnimations = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(IgnoreAnimations), false, "Ignore animations (always slide around)");
            PlantFeet = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(PlantFeet), false, "Feet stick to ground");
            
            FullBodyVrIk = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(FullBodyVrIk), true, "Enable IKTweaks (use custom VRIK)");
            AddHumanoidPass = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(AddHumanoidPass), true, "Enforce local NetIK (see what others see)");
            MapToes = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(MapToes), false, "Map toes (use if your feet trackers move with your toes)");

            UseKneeTrackers = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(UseKneeTrackers), false, "Use knee trackers");
            UseElbowTrackers = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(UseElbowTrackers), false, "Use elbow trackers");
            UseChestTracker = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(UseChestTracker), false, "Use chest tracker");
            
            CalibrateFollowHead = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(CalibrateFollowHead), true, "Avatar follows head when calibrating (recommended)");
            CalibrateHalfFreeze = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(CalibrateHalfFreeze), true, "Freeze avatar on one trigger hold in follow head mode");
            CalibrateUseUniversal = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(CalibrateUseUniversal), true, "Use universal calibration (requires follow head mode)");

            CalibrateStorePerAvatar = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(CalibrateStorePerAvatar), true, "Store calibration per avatar (when not using universal calibration)");
            DisableFbt = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(DisableFbt), false, "Disable FBT even if trackers are present");
            PinHipRotation = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(PinHipRotation), true, "Enforce hip rotation match");
            
            DoHipShifting = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(DoHipShifting), true, "Shift hip pivot (support inverted hip)");
            PreStraightenSpine = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(PreStraightenSpine), false, "Pre-straighten spine (improve IK stability)");
            StraightenNeck = (MelonPreferences_Entry<bool>) category.CreateEntry(nameof(StraightenNeck), true, "Straighten neck");
            
            SpineRelaxIterations = (MelonPreferences_Entry<int>) category.CreateEntry(nameof(SpineRelaxIterations), 10, "Spine Relax Iterations (max 25)");
            MaxSpineAngleFwd = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(MaxSpineAngleFwd), 30f, "Max spine angle forward (degrees)");
            MaxSpineAngleBack = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(MaxSpineAngleBack), 30f, "Max spine angle back (degrees)");

            MaxNeckAngleFwd = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(MaxNeckAngleFwd), 30f, "Max neck angle forward (degrees)");
            MaxNeckAngleBack = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(MaxNeckAngleBack), 15f, "Max neck angle back (degrees)");
            NeckPriority = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(NeckPriority), 2f, "Neck bend priority (1=even with spine, 2=twice as much as spine)");
            
            StraightSpineAngle = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(StraightSpineAngle), 15f, "Straight spine angle (degrees)");
            StraightSpinePower = (MelonPreferences_Entry<float>) category.CreateEntry(nameof(StraightSpinePower), 2f, "Straight spine power");
        }

        public static IKSolverVR.Arm.ShoulderRotationMode ShoulderMode => FixShoulders.Value ? IKSolverVR.Arm.ShoulderRotationMode.YawPitch : IKSolverVR.Arm.ShoulderRotationMode.FromTo;

        public static MelonPreferences_Entry<bool> FixShoulders;
        public static MelonPreferences_Entry<bool> CalibrateHalfFreeze;
        public static MelonPreferences_Entry<bool> CalibrateFollowHead;
        public static MelonPreferences_Entry<bool> CalibrateUseUniversal;
        public static MelonPreferences_Entry<bool> CalibrateStorePerAvatar;
        public static MelonPreferences_Entry<bool> UseKneeTrackers;
        public static MelonPreferences_Entry<bool> UseElbowTrackers;
        public static MelonPreferences_Entry<bool> UseChestTracker;
        public static MelonPreferences_Entry<bool> IgnoreAnimations;
        public static MelonPreferences_Entry<bool> PlantFeet;
        public static MelonPreferences_Entry<bool> FullBodyVrIk;
        public static MelonPreferences_Entry<bool> DisableFbt;
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
    }
}