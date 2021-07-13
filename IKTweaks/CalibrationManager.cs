using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Il2CppSystem.Text;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using Valve.VR;
using VRC.Core;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace IKTweaks
{
    public static class CalibrationManager
    {
        private static readonly Dictionary<string, Dictionary<CalibrationPoint, CalibrationData>> SavedAvatars = new Dictionary<string, Dictionary<CalibrationPoint, CalibrationData>>();

        private static readonly Dictionary<CalibrationPoint, CalibrationData> UniversalData = new Dictionary<CalibrationPoint, CalibrationData>();

        public static bool HasSavedCalibration(string avatarId) => SavedAvatars.ContainsKey(avatarId);
        public static void Clear()
        {
            SavedAvatars.Clear();
            UniversalData.Clear();
        }

        public static void ClearNonUniversal()
        {
            SavedAvatars.Clear();
        }

        public static void Clear(string avatarId)
        {
            SavedAvatars.Remove(avatarId);
            UniversalData.Clear();
        }

        public static void Save(string avatarId, CalibrationPoint point, CalibrationData data)
        {
            if (!SavedAvatars.ContainsKey(avatarId))
                SavedAvatars[avatarId] = new Dictionary<CalibrationPoint, CalibrationData>();

            SavedAvatars[avatarId][point] = data;
        }

        public struct CalibrationData
        {
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public string TrackerSerial;

            public CalibrationData(Vector3 localPosition, Quaternion localRotation, string trackerSerial)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                TrackerSerial = trackerSerial;
            }
        }

        public enum CalibrationPoint
        {
            Head,
            LeftHand,
            RightHand,
            Hip,
            LeftFoot,
            RightFoot,
            LeftKnee,
            RightKnee,
            LeftElbow,
            RightElbow,
            Chest
        }

        internal static SteamVR_ControllerManager GetControllerManager()
        {
            foreach (var vrcTracking in VRCTrackingManager.field_Private_Static_VRCTrackingManager_0
                .field_Private_List_1_VRCTracking_0)
            {
                var vrcTrackingSteam = vrcTracking.TryCast<VRCTrackingSteam>();
                if (vrcTrackingSteam == null) continue;

                return vrcTrackingSteam.field_Private_SteamVR_ControllerManager_0;
            }

            throw new ApplicationException("SteamVR tracking not found");
        }

        public static void Calibrate(GameObject avatarRoot)
        {
            CalibrateCore(avatarRoot).ContinueWith(t =>
            {
                if (t.Exception != null) 
                    MelonLogger.Error($"Task failed with exception: {t.Exception}");
            });
        }

        private static readonly float[] TPoseMuscles = {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.6001086f, 8.6213E-05f, -0.0003308152f,
            0.9999163f, -9.559652E-06f, 3.41413E-08f, -3.415095E-06f, -1.024528E-07f, 0.6001086f, 8.602679E-05f, -0.0003311098f,
            0.9999163f, -9.510122E-06f, 1.707468E-07f, -2.732077E-06f, 2.035554E-15f, -2.748694E-07f, 2.619475E-07f, 0.401967f,
            0.3005583f, 0.04102772f, 0.9998822f, -0.04634236f, 0.002522987f, 0.0003842837f, -2.369134E-07f, -2.232262E-07f,
            0.4019674f, 0.3005582f, 0.04103433f, 0.9998825f, -0.04634996f, 0.00252335f, 0.000383302f, -1.52127f, 0.2634507f,
            0.4322457f, 0.6443988f, 0.6669409f, -0.4663372f, 0.8116828f, 0.8116829f, 0.6678119f, -0.6186608f, 0.8116842f,
            0.8116842f, 0.6677991f, -0.619225f, 0.8116842f, 0.811684f, 0.6670032f, -0.465875f, 0.811684f, 0.8116836f, -1.520098f,
            0.2613016f, 0.432256f, 0.6444503f, 0.6668426f, -0.4670413f, 0.8116828f, 0.8116828f, 0.6677986f, -0.6192409f,
            0.8116841f, 0.811684f, 0.6677839f, -0.6198869f, 0.8116839f, 0.8116838f, 0.6668782f, -0.4667901f, 0.8116842f, 0.811684f
        };

        private static readonly float[] APoseMuscles =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0.6001087f, 0f,
            -0.0003306383f, 0.9999163f, 0f, 0f, 0f, 0f, 0.6001087f, 0f, -0.0003306384f, 0.9999163f, 0f, 0f, 0f, 0f, 0f,
            0f, -0.1071228f, 0.258636f, 0.1567371f, 0.9998825f, -0.0463457f, 0.002523606f, 0.0003833446f, 0f, 0f,
            -0.1036742f, 0.2589961f, 0.1562322f, 0.9998825f, -0.04634446f, 0.002522176f, 0.0003835156f, -1.52127f,
            0.2634749f, 0.4322476f, 0.6443989f, 0.6669405f, -0.4663376f, 0.8116828f, 0.8116829f, 0.6678116f,
            -0.6186616f, 0.8116839f, 0.8116837f, 0.6677991f, -0.6192248f, 0.8116839f, 0.8116842f, 0.6670038f,
            -0.4658763f, 0.8116841f, 0.811684f, -1.520108f, 0.2612858f, 0.4322585f, 0.6444519f, 0.6668428f, -0.4670413f,
            0.8116831f, 0.8116828f, 0.6677985f, -0.6192364f, 0.8116842f, 0.8116842f, 0.667784f, -0.6198866f, 0.8116841f,
            0.8116835f, 0.6668782f, -0.4667891f, 0.8116841f, 0.811684f
        };
        
        private static string? GetTrackerSerial(int trackerId)
        {
            var sb = new StringBuilder(64);
            ETrackedPropertyError err = ETrackedPropertyError.TrackedProp_Success;
            OpenVR.System.GetStringTrackedDeviceProperty((uint) trackerId, ETrackedDeviceProperty.Prop_SerialNumber_String, sb, (uint) sb.Capacity, ref err);
            if (err == ETrackedPropertyError.TrackedProp_Success)
                return sb.ToString();
            
            MelonLogger.Warning($"Can't get serial for tracker ID {trackerId}");
            return null;
        }
        
        private static Transform? FindTracker(string serial, SteamVR_ControllerManager? steamVrControllerManager)
        {
            steamVrControllerManager ??= GetControllerManager();

            return steamVrControllerManager.field_Public_ArrayOf_GameObject_0
                .Where(it => it != steamVrControllerManager.field_Public_GameObject_0 && it != steamVrControllerManager.field_Public_GameObject_1 && it != null)
                .First(it => GetTrackerSerial((int) it.GetComponent<SteamVR_TrackedObject>().field_Public_EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique_0) == serial)
                .transform;
        }

        private static GameObject[] ourTargets = new GameObject[0];
        private static async Task ApplyStoredCalibration(GameObject avatarRoot, string avatarId)
        {
            // await IKTweaksMod.AwaitLateUpdate();

            var dummyMuscles = new Il2CppStructArray<float>(HumanTrait.MuscleCount);

            // Enforce mostly-bike-pose for IK setup - otherwise animations can break knee bend angles and the like
            // Some avatars apparently have inverted knees in T-pose, so bike pose is preferred here
            var animator = avatarRoot.GetComponent<Animator>();
            var poseHandler = new HumanPoseHandler(animator.avatar, avatarRoot.transform);
            poseHandler.GetHumanPose(out var position, out var rotation, dummyMuscles);
            rotation = Quaternion.identity;
            for (var i = 0; i < FullBodyHandling.ourBoneResetMasks.Length; i++)
            {
                if (FullBodyHandling.ourBoneResetMasks[i] != FullBodyHandling.BoneResetMask.Never)
                    dummyMuscles[i] = 0;
            }

            poseHandler.SetHumanPose(ref position, ref rotation, dummyMuscles);
            
            FullBodyHandling.PreSetupVrIk(avatarRoot);
            var vrik = FullBodyHandling.SetupVrIk(FullBodyHandling.LastInitializedController, avatarRoot);

            foreach (var target in ourTargets)
                Object.DestroyImmediate(target);


            var steamVrControllerManager = GetControllerManager();
            var newTargets = new List<GameObject>();

            var datas = SavedAvatars[avatarId];

            Transform? GetTarget(CalibrationPoint point)
            {
                if (!datas.TryGetValue(point, out var data))
                    return null;

                var bestTracker = FindTracker(data.TrackerSerial, steamVrControllerManager);

                if (bestTracker == null)
                {
                    MelonLogger.Msg($"Null target for tracker {data.TrackerSerial}");
                    return null;
                }

                MelonLogger.Msg($"Found tracker with serial {data.TrackerSerial} for point {point}");

                var result = bestTracker;

                var newTarget = new GameObject("CustomIkTarget-For-" + data.TrackerSerial + "-" + point);
                newTargets.Add(newTarget);
                var targetTransform = newTarget.transform;
                targetTransform.SetParent(result);
                targetTransform.localPosition = data.LocalPosition;
                targetTransform.localRotation = data.LocalRotation;

                return targetTransform;
            }

            Transform MakeHandTarget(Quaternion localRotation, Transform parent)
            {
                var targetGo = new GameObject("CustomIkHandTarget");
                var targetTransform = targetGo.transform;
                targetTransform.SetParent(parent, false);
                targetTransform.localRotation = localRotation;

                newTargets.Add(targetGo);
                return targetTransform;
            }

            var hips = GetTarget(CalibrationPoint.Hip);
            var leftFoot = GetTarget(CalibrationPoint.LeftFoot);
            var rightFoot = GetTarget(CalibrationPoint.RightFoot);

            vrik.solver.leftArm.target = MakeHandTarget(datas[CalibrationPoint.LeftHand].LocalRotation,
                FullBodyHandling.LastInitializedController.field_Private_FullBodyBipedIK_0.solver.leftHandEffector.target);
            vrik.solver.rightArm.target = MakeHandTarget(datas[CalibrationPoint.RightHand].LocalRotation,
                FullBodyHandling.LastInitializedController.field_Private_FullBodyBipedIK_0.solver.rightHandEffector.target);

            vrik.solver.leftLeg.bendGoal = GetTarget(CalibrationPoint.LeftKnee);
            vrik.solver.rightLeg.bendGoal = GetTarget(CalibrationPoint.RightKnee);

            vrik.solver.leftArm.bendGoal = GetTarget(CalibrationPoint.LeftElbow);
            vrik.solver.rightArm.bendGoal = GetTarget(CalibrationPoint.RightElbow);

            vrik.solver.spine.chestGoal = GetTarget(CalibrationPoint.Chest);

            ourTargets = newTargets.ToArray();

            vrik.solver.spine.pelvisTarget = hips;
            vrik.solver.leftLeg.target = leftFoot;
            vrik.solver.rightLeg.target = rightFoot;

            MelonLogger.Msg("Applied stored calibration");
        }

        private static Action<VRCTrackingSteam, bool>? ourSetVisibilityDelegate;
        private static void SetTrackerVisibility(bool visible)
        {
            if (ourSetVisibilityDelegate == null)
            {
                var method = typeof(VRCTrackingSteam)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single(it =>
                        it.ReturnType == typeof(void) && it.GetParameters().Length == 1 &&
                        it.GetParameters()[0].ParameterType == typeof(bool) && XrefScanner.XrefScan(it)
                            .Any(jt => jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "Model"));

                ourSetVisibilityDelegate = (Action<VRCTrackingSteam, bool>) Delegate.CreateDelegate(typeof(Action<VRCTrackingSteam, bool>), method);
            }
            
            foreach (var vrcTracking in VRCTrackingManager.field_Private_Static_VRCTrackingManager_0.field_Private_List_1_VRCTracking_0)
            {
                var vrcTrackingSteam = vrcTracking.TryCast<VRCTrackingSteam>();
                if (vrcTrackingSteam == null) continue;

                ourSetVisibilityDelegate(vrcTrackingSteam, visible);
                return;
            }
        }

        private static void MoveTrackersToStoredPositions()
        {
            var ikController = FullBodyHandling.LastInitializedController;
            var headTracker = ikController.field_Private_FBBIKHeadEffector_0.transform.parent;
            var currentHeadForwardProjected = Vector3.ProjectOnPlane(headTracker.forward, Vector3.up);
            var steamVrControllerManager = GetControllerManager();
            var trackersParent = steamVrControllerManager.field_Public_ArrayOf_GameObject_0[3].transform.parent;
            
            var headData = UniversalData[CalibrationPoint.Head];

            headTracker.position = trackersParent.TransformPoint(headData.LocalPosition);
            headTracker.rotation = headData.LocalRotation * trackersParent.rotation;

            var newHeadForwardProjected = Vector3.ProjectOnPlane(headTracker.forward, Vector3.up);

            var rotation = Quaternion.FromToRotation(newHeadForwardProjected, currentHeadForwardProjected);
            rotation.ToAngleAxis(out var angle, out var axis);

            var headTrackerPosition = headTracker.position;
            headTracker.RotateAround(headTrackerPosition, axis, angle);
            
            void DoConvert(CalibrationPoint point, ref float weightOut)
            {
                weightOut = 0f;
                
                if (!UniversalData.TryGetValue(point, out var data)) return;

                var tracker = FindTracker(data.TrackerSerial, steamVrControllerManager);
                if (tracker == null) return;
                
                tracker.localPosition = data.LocalPosition;
                tracker.localRotation = data.LocalRotation;
                    
                tracker.RotateAround(headTrackerPosition, axis, angle);

                weightOut = 1;
            }

            float _ = 0f;
            DoConvert(CalibrationPoint.Hip, ref _);
            DoConvert(CalibrationPoint.LeftFoot, ref _);
            DoConvert(CalibrationPoint.RightFoot, ref _);
            DoConvert(CalibrationPoint.LeftElbow, ref FullBodyHandling.LeftElbowWeight);
            DoConvert(CalibrationPoint.RightElbow, ref FullBodyHandling.RightElbowWeight);
            DoConvert(CalibrationPoint.LeftKnee, ref FullBodyHandling.LeftKneeWeight);
            DoConvert(CalibrationPoint.RightKnee, ref FullBodyHandling.RightKneeWeight);
            DoConvert(CalibrationPoint.Chest, ref FullBodyHandling.ChestWeight);
        }

        private static async Task CalibrateCore(GameObject avatarRoot)
        {
            var avatarId = avatarRoot.GetComponent<PipelineManager>().blueprintId;
            if (IkTweaksSettings.CalibrateStorePerAvatar.Value && HasSavedCalibration(avatarId))
            {
                await ApplyStoredCalibration(avatarRoot, avatarId);
                return;
            }

            await ManualCalibrateCoro(avatarRoot);
            
            await IKTweaksMod.AwaitVeryLateUpdate();

            if (!avatarRoot) return;
            
            await ApplyStoredCalibration(avatarRoot, avatarId);
        }

        private static Vector3 GetLocalPosition(Transform parent, Transform child) => parent.InverseTransformPoint(child.position);
        private static Vector3 GetLocalPosition(Transform parent, Vector3 childPosition) => parent.InverseTransformPoint(childPosition);
        private static Quaternion GetLocalRotation(Transform parent, Transform child) => Quaternion.Inverse(parent.rotation) * child.rotation;
        private static Quaternion GetLocalRotation(Transform parent, Quaternion childRotation) => Quaternion.Inverse(parent.rotation) * childRotation;

        private static async Task ManualCalibrateCoro(GameObject avatarRoot)
        {
            var animator = avatarRoot.GetComponent<Animator>();
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var avatarId = avatarRoot.GetComponent<PipelineManager>().blueprintId;
            var avatarRootTransform = avatarRoot.transform;
            var nativeMuscles = (Il2CppStructArray<float>) (IkTweaksSettings.APoseCalibration.Value ? APoseMuscles : TPoseMuscles);
            var dummyMuscles = new Il2CppStructArray<float>(nativeMuscles.Count);
            var poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);

            var forwardDirection = avatarRootTransform.parent;
            var hipsRelativeToForwardDirection = Quaternion.Inverse(forwardDirection.rotation) * hips.rotation;
            
            MelonDebug.Msg($"Calibrating for avatar ID {avatarId}");
            
            var oldHipPos = hips.position;
            var oldHipRot = hips.rotation;
            
            await IKTweaksMod.AwaitVeryLateUpdate();

            if (!avatarRoot)
            {
                MelonDebug.Msg("Avatar root was destroyed, cancelling calibration");
                return;
            }

            var headTarget = FullBodyHandling.LastInitializedController.field_Private_FBBIKHeadEffector_0.transform;
            var headsetTracker = headTarget.parent;
            
            SetTrackerVisibility(true);

            var preClickHeadPos = Vector3.zero;
            var preClickHeadRot = Quaternion.identity;

            var mirrorCloneRoot = forwardDirection.Find("_AvatarMirrorClone");
            HumanPoseHandler mirrorClonePoseHandler = null;
            Transform mirrorHips = null;
            if (mirrorCloneRoot != null)
            {
                mirrorClonePoseHandler = new HumanPoseHandler(animator.avatar, mirrorCloneRoot);
                var mirrorCloneAnimator = mirrorCloneRoot.GetComponent<Animator>();
                if (mirrorCloneAnimator != null) mirrorHips = mirrorCloneAnimator.GetBoneTransform(HumanBodyBones.Hips);
            }
            
            var willUniversallyCalibrate = false;
            
            var triggerInput1 = VRCInputManager.Method_Public_Static_VRCInput_String_0("UseLeft");
            var triggerInput2 = VRCInputManager.Method_Public_Static_VRCInput_String_0("UseRight");

            while (true)
            {
                await IKTweaksMod.AwaitIKLateUpdate();
                if (avatarRoot == null) break;
                
                poseHandler.GetHumanPose(out var humanBodyPose, out var humanBodyRot, dummyMuscles);
                poseHandler.SetHumanPose(ref humanBodyPose, ref humanBodyRot, nativeMuscles);
                mirrorClonePoseHandler?.SetHumanPose(ref humanBodyPose, ref humanBodyRot, nativeMuscles);

                var trigger1 = triggerInput1.prop_Single_0;
                var trigger2 = triggerInput2.prop_Single_0;
                
                if (IkTweaksSettings.CalibrateUseUniversal.Value && UniversalData.Count >= 4)
                {
                    MoveTrackersToStoredPositions();
                    willUniversallyCalibrate = true;
                }

                if (IkTweaksSettings.CalibrateHalfFreeze.Value && trigger1 + trigger2 > 0.75f)
                {
                    hips.position = oldHipPos;
                    hips.rotation = oldHipRot;
                    
                    if (mirrorHips != null)
                    {
                        mirrorHips.position = oldHipPos;
                        mirrorHips.rotation = oldHipRot;
                    }
                }
                else if(IkTweaksSettings.CalibrateFollowHead.Value || willUniversallyCalibrate)
                {
                    hips.rotation = forwardDirection.rotation * hipsRelativeToForwardDirection;
                    
                    var delta = headTarget.position - head.position;
                    hips.position += delta;
                    if (mirrorHips != null) mirrorHips.position = hips.position;
                    oldHipPos = hips.position;
                    oldHipRot = hips.rotation;
                    
                    preClickHeadPos = headsetTracker.position;
                    preClickHeadRot = headsetTracker.rotation;
                }
                else
                { 
                    // legacy calibration
                    hips.position = oldHipPos;
                    hips.rotation = oldHipRot;
                    
                    if (mirrorHips != null)
                    {
                        mirrorHips.position = oldHipPos;
                        mirrorHips.rotation = oldHipRot;
                    }
                    
                    preClickHeadPos = headsetTracker.position;
                    preClickHeadRot = headsetTracker.rotation;
                }

                if (trigger1 + trigger2 > 1.75f || willUniversallyCalibrate)
                {
                    break;
                }
            }
            
            SetTrackerVisibility(false);

            if (avatarRoot == null)
                return;

            var steamVrControllerManager = GetControllerManager();
            var possibleTrackers = new List<Transform>(steamVrControllerManager.field_Public_ArrayOf_GameObject_0
                .Where(it => it != steamVrControllerManager.field_Public_GameObject_1 && it != steamVrControllerManager.field_Public_GameObject_0 && it != null)
                .Select(it => it.transform));

            (Transform Tracker, Transform Bone)? GetTracker(HumanBodyBones bone, HumanBodyBones fallback = HumanBodyBones.Hips)
            {
                var boneTransform = animator.GetBoneTransform(bone) ?? animator.GetBoneTransform(fallback);
                var bonePosition = boneTransform.position;
                var bestTracker = -1;
                var bestDistance = float.PositiveInfinity;
                for (var index = 0; index < possibleTrackers.Count; index++)
                {
                    var possibleTracker = possibleTrackers[index];
                    var steamVRTrackedObject = possibleTracker.GetComponent<SteamVR_TrackedObject>();
                    if (steamVRTrackedObject.field_Public_EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique_0 ==
                        SteamVR_TrackedObject.EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique.None)
                        continue;

                    var distance = Vector3.Distance(possibleTracker.position, bonePosition);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTracker = index;
                    }
                }

                if (bestTracker == -1)
                {
                    MelonLogger.Msg($"Null target for bone {bone}");
                    return null;
                }

                var result = possibleTrackers[bestTracker]!;
                possibleTrackers.RemoveAt(bestTracker);

                return (result, boneTransform);
            }

            void StoreData(CalibrationPoint point, (Transform tracker, Transform bone)? pair)
            {
                if (pair == null) return;
                var tracker = pair.Value.tracker;
                var bone = pair.Value.bone;

                var serial = GetTrackerSerial((int) tracker.GetComponent<SteamVR_TrackedObject>().field_Public_EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique_0);

                var trackerRelativeData = new CalibrationData(GetLocalPosition(tracker, bone),
                    GetLocalRotation(tracker, bone), serial);

                Save(avatarId, point, trackerRelativeData);

                if (!willUniversallyCalibrate)
                {
                    var avatarSpaceData = new CalibrationData(GetLocalPosition(tracker.parent, tracker),
                        GetLocalRotation(tracker.parent, tracker), serial);
                    UniversalData[point] = avatarSpaceData;
                }
            }

            void StoreHand(Vector3 angles, HumanBodyBones handBone, CalibrationPoint point)
            {
                var handRotation = animator.GetBoneTransform(handBone).rotation;
                var bodyRotation = animator.transform.rotation;

                var storedData = new CalibrationData(Vector3.zero,
                    Quaternion.Euler(angles) * Quaternion.Inverse(bodyRotation) * handRotation, point.ToString());

                Save(avatarId, point, storedData);
            }

            void StoreBendGoal(CalibrationPoint point, (Transform tracker, Transform bone)? pair, Vector3 offset, ref float weight)
            {
                weight = 0f;
                if (pair == null) return;

                var tracker = pair.Value.tracker;
                var bone = pair.Value.bone;

                var serial = GetTrackerSerial((int) tracker.GetComponent<SteamVR_TrackedObject>().field_Public_EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique_0);

                var trackerRelativeData = new CalibrationData(tracker.InverseTransformPoint(bone.position + offset),
                    Quaternion.identity, serial);

                Save(avatarId, point, trackerRelativeData);

                if (!willUniversallyCalibrate)
                {
                    var avatarSpaceData = new CalibrationData(GetLocalPosition(tracker.parent, tracker),
                        GetLocalRotation(tracker.parent, tracker), serial);
                    UniversalData[point] = avatarSpaceData;
                }

                weight = 1f;
            }

            var hipsTracker = GetTracker(HumanBodyBones.Hips);
            var leftFootTracker =
                GetTracker(IkTweaksSettings.MapToes.Value ? HumanBodyBones.LeftToes : HumanBodyBones.LeftFoot,
                    HumanBodyBones.LeftFoot);
            var rightFootTracker =
                GetTracker(IkTweaksSettings.MapToes.Value ? HumanBodyBones.RightToes : HumanBodyBones.RightFoot,
                    HumanBodyBones.RightFoot);

            StoreData(CalibrationPoint.Hip, hipsTracker);
            StoreData(CalibrationPoint.LeftFoot, leftFootTracker);
            StoreData(CalibrationPoint.RightFoot, rightFootTracker);

            var leftLowerLegPosition = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
            var rightLowerLegPosition = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position;
            var avatarForward = Vector3.Cross(rightLowerLegPosition - leftLowerLegPosition, Vector3.up).normalized;

            if (IkTweaksSettings.UseElbowTrackers.Value)
            {
                var leftElbowTracker = GetTracker(HumanBodyBones.LeftLowerArm);
                var rightElbowTracker = GetTracker(HumanBodyBones.RightLowerArm);

                StoreBendGoal(CalibrationPoint.LeftElbow, leftElbowTracker, avatarForward * -0.1f, ref FullBodyHandling.LeftElbowWeight);
                StoreBendGoal(CalibrationPoint.RightElbow, rightElbowTracker, avatarForward * -0.1f, ref FullBodyHandling.RightElbowWeight);
            }

            if (IkTweaksSettings.UseKneeTrackers.Value)
            {
                var leftKneeTracker = GetTracker(HumanBodyBones.LeftLowerLeg);
                var rightKneeTracker = GetTracker(HumanBodyBones.RightLowerLeg);

                StoreBendGoal(CalibrationPoint.LeftKnee, leftKneeTracker, avatarForward * 0.1f, ref FullBodyHandling.LeftKneeWeight);
                StoreBendGoal(CalibrationPoint.RightKnee, rightKneeTracker, avatarForward * 0.1f, ref FullBodyHandling.RightKneeWeight);
            }

            if (IkTweaksSettings.UseChestTracker.Value)
            {
                var chestTracker = GetTracker(HumanBodyBones.UpperChest, HumanBodyBones.Chest);

                StoreBendGoal(CalibrationPoint.Chest, chestTracker, avatarForward * .5f, ref FullBodyHandling.ChestWeight);
            }

            if (!willUniversallyCalibrate)
            {
                var trackerParent = hipsTracker.Value.Tracker.parent;
                
                UniversalData[CalibrationPoint.Head] = new CalibrationData(
                    GetLocalPosition(trackerParent, preClickHeadPos),
                    GetLocalRotation(trackerParent, preClickHeadRot), "HEAD");
            }

            if (IkTweaksSettings.APoseCalibration.Value) 
            { // enforce T-pose for hands calibration 
                nativeMuscles = TPoseMuscles;
                poseHandler.GetHumanPose(out var humanBodyPose, out var humanBodyRot, dummyMuscles);
                poseHandler.SetHumanPose(ref humanBodyPose, ref humanBodyRot, nativeMuscles);
            }
            
            StoreHand(new Vector3(15, 90 + 10, 0), HumanBodyBones.LeftHand, CalibrationPoint.LeftHand);
            StoreHand(new Vector3(15, -90 - 10, 0), HumanBodyBones.RightHand, CalibrationPoint.RightHand);
        }
    }
}