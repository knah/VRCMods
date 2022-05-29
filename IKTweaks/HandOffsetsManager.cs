using System;
using UnityEngine;
using VRC.SDKBase;

namespace IKTweaks
{
    public class HandOffsetsManager: IDisposable
    {
        private readonly Transform myLeftHandOffset;
        private readonly Transform myRightHandOffset;

        private readonly float myOriginalScale;

        public HandOffsetsManager(VRCVrIkController controller)
        {
            var ikControllerParent = controller.field_Private_IkController_0.transform;
            
            var leftEffector = ikControllerParent.Find("LeftEffector");
            var rightEffector = ikControllerParent.Find("RightEffector");

            myLeftHandOffset = MakeTarget(leftEffector);
            myRightHandOffset = MakeTarget(rightEffector);

            var vrik = controller.field_Private_VRIK_0;
            vrik.solver.leftArm.target = myLeftHandOffset;
            vrik.solver.rightArm.target = myRightHandOffset;

            myOriginalScale = vrik.GetComponent<VRC_AvatarDescriptor>().ViewPosition.y;
            
            UpdatePositionOffset(Vector3.zero, IkTweaksSettings.HandPositionOffset.Value);
            UpdateRotationOffset(Vector3.zero, IkTweaksSettings.HandAngleOffset.Value);
        }

        internal void UpdatePositionOffset(Vector3 _, Vector3 newOffset)
        {
            newOffset *= myOriginalScale;
            
            myLeftHandOffset.localPosition = newOffset;
            myRightHandOffset.localPosition = new Vector3 { x = -newOffset.x, y = newOffset.y, z = newOffset.z };
        }

        internal void UpdateRotationOffset(Vector3 _, Vector3 newOffset)
        {
            myLeftHandOffset.localEulerAngles = newOffset;
            myRightHandOffset.localEulerAngles = new Vector3 { x = newOffset.x, y = -newOffset.y, z = -newOffset.z };
        }

        private static Transform MakeTarget(Transform parent)
        {
            var newXf = new GameObject("IktOffset").transform;
            newXf.SetParent(parent, false);
            return newXf;
        }

        public void Dispose()
        {
            if (myLeftHandOffset != null) UnityEngine.Object.Destroy(myLeftHandOffset.gameObject); 
            if (myRightHandOffset != null) UnityEngine.Object.Destroy(myRightHandOffset.gameObject); 
        }
    }
}