using System;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;

namespace IKTweaks
{
    public class WallFreezeFixer
    {
        private readonly Transform myLeftEffector;
        private readonly Transform myRightEffector;
        private readonly Transform myHeadEffector;
        
        private static Func<VRCTracking.ID, Transform> ourGetTrackedTransform;

        private static Transform GetTrackedTransform(VRCTracking.ID id)
        {
            ourGetTrackedTransform ??= (Func<VRCTracking.ID, Transform>)Delegate.CreateDelegate(
                typeof(Func<VRCTracking.ID, Transform>), typeof(VRCTrackingManager)
                    .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly).Single(it =>
                        it.Name.StartsWith("Method_Public_Static_Transform_ID_") && XrefScanner.UsedBy(it)
                            .Any(
                                jt =>
                                {
                                    var mr = jt.TryResolve();
                                    return mr?.DeclaringType == typeof(PedalOption_HudPosition) && mr.Name == "Update";
                                })));

            return ourGetTrackedTransform(id);
        }

        public WallFreezeFixer(VRCVrIkController controller)
        {
            var ikControllerParent = controller.field_Private_IkController_0.transform;
            myLeftEffector = ikControllerParent.Find("LeftEffector");
            myRightEffector = ikControllerParent.Find("RightEffector");
            myHeadEffector = ikControllerParent.Find("HeadEffector");
        }
        
        internal ControllerPositionsCookie GetCookie()
        {
            return new ControllerPositionsCookie(this);
        }

        internal struct ControllerPositionsCookie : IDisposable
        {
            private readonly WallFreezeFixer myFixer;
            
            private (Vector3 position, Quaternion rotation) myOldHeadPosition;
            private (Vector3 position, Quaternion rotation) myOldLeftPosition;
            private (Vector3 position, Quaternion rotation) myOldRightPosition;

            private bool myMoved;

            public ControllerPositionsCookie(WallFreezeFixer fixer)
            {
                myFixer = fixer;
                myMoved = false;

                myOldHeadPosition = myOldLeftPosition = myOldRightPosition = default;
            }

            public void MoveTargets()
            {
                if (!IkTweaksSettings.NoWallFreeze.Value) return;
                
                myFixer.myHeadEffector.get_position_Injected(out myOldHeadPosition.Item1);
                myFixer.myHeadEffector.get_rotation_Injected(out myOldHeadPosition.Item2);
                
                myFixer.myLeftEffector.get_position_Injected(out myOldLeftPosition.Item1);
                myFixer.myLeftEffector.get_rotation_Injected(out myOldLeftPosition.Item2);
                
                myFixer.myRightEffector.get_position_Injected(out myOldRightPosition.Item1);
                myFixer.myRightEffector.get_rotation_Injected(out myOldRightPosition.Item2);
                
                myMoved = true;
                
                var headTracker = GetTrackedTransform(VRCTracking.ID.Hmd);
                var leftTracker = GetTrackedTransform(VRCTracking.ID.HandTracker_LeftWrist);
                var rightTracker = GetTrackedTransform(VRCTracking.ID.HandTracker_RightWrist);

                myFixer.myLeftEffector.SetPositionAndRotation(leftTracker.position, leftTracker.rotation);
                myFixer.myRightEffector.SetPositionAndRotation(rightTracker.position, rightTracker.rotation);
                myFixer.myHeadEffector.SetPositionAndRotation(headTracker.position, headTracker.rotation);
            }

            public void Dispose()
            {
                if (!myMoved) return;
                
                // only return hands to maintain pickup behavior, but not head as the solver really needs it to be where it belongs
                myFixer.myLeftEffector.SetPositionAndRotation(myOldLeftPosition.position, myOldLeftPosition.rotation);
                myFixer.myRightEffector.SetPositionAndRotation(myOldRightPosition.position, myOldRightPosition.rotation);
            }
        }
    }
}