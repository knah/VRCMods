using System.Reflection;
using MelonLoader;
using RootMotion.FinalIK;

namespace IKTweaks
{
    public static class IkTweaksSettings
    {
        private const string IkTweaksCategory = "IkTweaks";
        
        private const string FixShoulders = "PitchYawShoulders";

        internal static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(IkTweaksCategory, "IK Tweaks");
            
            MelonPrefs.RegisterBool(IkTweaksCategory, FixShoulders, true, "Use Pitch-Yaw Shoulders");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(IgnoreAnimations), true, "Ignore animations (always slide around)");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(PlantFeet), false, "Feet stick to ground");
            
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(FullBodyVrIk), true, "Use custom VRIK for FBT");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(AddHumanoidPass), true, "Enforce local NetIK (see what others see)");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(MapToes), false, "Map toes (use if your feet trackers move with your toes)");

            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(UseKneeTrackers), false, "Use knee trackers");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(UseElbowTrackers), false, "Use elbow trackers");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(UseChestTracker), false, "Use chest tracker");
            
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(CalibrateFollowHead), true, "Avatar follows head when calibrating (recommended)");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(CalibrateHalfFreeze), true, "Freeze avatar on one trigger hold in follow head mode");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(CalibrateUseUniversal), true, "Use universal calibration (requires follow head mode)");

            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(CalibrateStorePerAvatar), true, "Store calibration per avatar (when not using universal calibration)");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(DisableFbt), false, "Disable FBT even if trackers are present");
            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(PinHipRotation), false, "Enforce hip rotation match");

            MelonPrefs.RegisterBool(IkTweaksCategory, nameof(StraightenNeck), false, "Straighten neck");
            MelonPrefs.RegisterInt(IkTweaksCategory, nameof(SpineRelaxIterations), 10, "Spine Relax Iterations (max 25)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(MaxSpineAngleFwd), 30, "Max spine angle forward (degrees)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(MaxSpineAngleBack), 30, "Max spine angle back (degrees)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(MaxNeckAngleFwd), 30, "Max neck angle forward (degrees)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(MaxNeckAngleBack), 15, "Max neck angle back (degrees)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(NeckPriority), 2f, "Neck bend priority (1=even with spine, 2=twice as much as spine)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(StraightSpineAngle), 15f, "Straight spine angle (degrees)");
            MelonPrefs.RegisterFloat(IkTweaksCategory, nameof(StraightSpinePower), 2, "Straight spine power");

            OnSettingsApplied();
        }

        public static IKSolverVR.Arm.ShoulderRotationMode ShoulderMode { get; private set; } = IKSolverVR.Arm.ShoulderRotationMode.FromTo;

        public static bool CalibrateHalfFreeze;
        public static bool CalibrateFollowHead;
        public static bool CalibrateUseUniversal;
        public static bool CalibrateStorePerAvatar;

        public static bool UseKneeTrackers;
        public static bool UseElbowTrackers;
        public static bool UseChestTracker;

        public static bool IgnoreAnimations;
        public static bool PlantFeet;
        public static bool FullBodyVrIk;
        public static bool DisableFbt;
        
        public static float MaxSpineAngleFwd;
        public static float MaxSpineAngleBack;
        public static int SpineRelaxIterations;
        public static float MaxNeckAngleFwd;
        public static float MaxNeckAngleBack;
        public static float NeckPriority;
        public static bool AddHumanoidPass;
        public static bool MapToes;
        public static bool StraightenNeck;
        public static float StraightSpineAngle;
        public static float StraightSpinePower;
        public static bool PinHipRotation;

        internal static void OnSettingsApplied()
        {
            ShoulderMode = MelonPrefs.GetBool(IkTweaksCategory, FixShoulders)
                ? IKSolverVR.Arm.ShoulderRotationMode.YawPitch
                : IKSolverVR.Arm.ShoulderRotationMode.FromTo;

            foreach (var fieldInfo in typeof(IkTweaksSettings).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (fieldInfo.FieldType == typeof(string))
                {
                    fieldInfo.SetValue(null, MelonPrefs.GetString(IkTweaksCategory, fieldInfo.Name));
                } else if (fieldInfo.FieldType == typeof(bool))
                {
                    fieldInfo.SetValue(null, MelonPrefs.GetBool(IkTweaksCategory, fieldInfo.Name));
                } else if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(null, MelonPrefs.GetFloat(IkTweaksCategory, fieldInfo.Name));
                } else if (fieldInfo.FieldType == typeof(int))
                {
                    fieldInfo.SetValue(null, MelonPrefs.GetInt(IkTweaksCategory, fieldInfo.Name));
                }
            }

            if (SpineRelaxIterations > 25)
                SpineRelaxIterations = 25;
        }
    }
}