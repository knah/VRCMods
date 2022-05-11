using System;
using System.Runtime.CompilerServices;
using RootMotion.FinalIK;
using UnityEngine;

namespace ScaleGoesBrr
{
    public class ScaleGoesBrrComponent : MonoBehaviour
    {
        public Vector3 originalSourceScale;
        public Vector3 originalTargetPsScale;
        public Vector3 originalTargetAlScale;
        public Vector3 originalTargetUiScale;
        public Vector3 originalTargetUiInvertedScale;
        public Transform source;
        public Transform targetPs;
        public Transform targetAl;
        public Transform targetUi;
        public Transform targetUiInverted;
        public Transform targetVpParent;
        public Transform targetVp;
        public Transform targetHandParentL;
        public Transform targetHandParentR;

        public Transform RootFix;

        public IKSolverVR.Locomotion vrik;
        public float originalStep;

        public VRCAvatarManager avatarManager;
        public float amSingle0;
        public float amSingle1;
        public float amSingle3;
        public float amSingle4;
        public float amSingle5;
        
        public Vector3 tmSV0;
        public Vector3 tmSV1;
        public bool tmReady;

        private float lastScaleFactor = 1;

        // Some mods instantiate extra copies of local avatar. This will be always false if Unity "clones" this component
        public bool ActuallyDoThings;

        public ScaleGoesBrrComponent(IntPtr ptr) : base(ptr)
        {
        }

        private static Vector3 Scale(Vector3 original, float scale)
        {
            return new Vector3
            {
                x = original.x * scale,
                y = original.y * scale,
                z = original.z * scale,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoScale(float scaleFactor, Vector3 originalTargetScale, Transform target)
        {
            var neededTargetScale = Scale(originalTargetScale, scaleFactor);
            target.localScale = neededTargetScale;
        }

        private void OnDestroy()
        {
            if (targetVpParent != null) targetVpParent.localScale = Vector3.one;
            if (targetHandParentL != null) targetHandParentL.localScale = Vector3.one;
            if (targetHandParentR != null) targetHandParentR.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            if (!ActuallyDoThings) return;

            var fixPsCenterBias = ScaleGoesBrrMod.FixPlayspaceCenterBias.Value;
            Vector3 originalPsToAvOffset = default;
            Vector3 originalAvPosition = default;
            if (fixPsCenterBias)
            {
                source.get_position_Injected(out originalAvPosition);
                targetPs.InverseTransformPoint_Injected(ref originalAvPosition, out originalPsToAvOffset);
            }

            source.get_localScale_Injected(out var sourceScale);
            var scaleFactor = sourceScale.y / originalSourceScale.y;
            DoScale(scaleFactor, originalTargetPsScale, targetPs);
            DoScale(1 / scaleFactor, originalTargetAlScale, targetAl);
            DoScale(scaleFactor, originalTargetUiScale, targetUi);
            DoScale(1 / scaleFactor, originalTargetUiInvertedScale, targetUiInverted);

            var scaleVector = new Vector3
            {
                x = scaleFactor,
                y = scaleFactor,
                z = scaleFactor,
            };
            targetVpParent.localScale = scaleVector;
            targetHandParentL.localScale = scaleVector;
            targetHandParentR.localScale = scaleVector;

            avatarManager.field_Private_Single_0 = amSingle0 * scaleFactor;
            avatarManager.field_Private_Single_1 = amSingle1 * scaleFactor;
            avatarManager.field_Private_Single_3 = amSingle3 * scaleFactor;
            avatarManager.field_Private_Single_4 = amSingle4 * scaleFactor;
            avatarManager.field_Private_Single_5 = amSingle5 * scaleFactor;

            if (!tmReady) return;
            
            VRCTrackingManager.field_Private_Static_Vector3_0 = Scale(tmSV0, scaleFactor);
            VRCTrackingManager.field_Private_Static_Vector3_1 = Scale(tmSV1, scaleFactor);

            if (Math.Abs(scaleFactor - lastScaleFactor) > lastScaleFactor / 100)
            {
                lastScaleFactor = scaleFactor;
                targetVp.get_localPosition_Injected(out var vpOffset);
                vpOffset = Scale(vpOffset, scaleFactor); // it will be applied by VpParent scale
                ScaleGoesBrrMod.UpdateCameraOffsetForScale(vpOffset);
                vrik.footDistance = originalStep * scaleFactor;
                ScaleGoesBrrMod.FireScaleChange(source, scaleFactor);
            }

            if (fixPsCenterBias)
            {
                targetPs.TransformPoint_Injected(ref originalPsToAvOffset, out var newAvPosition);
                targetPs.get_position_Injected(out var originalPsPosition);
                var newPsPosition = new Vector3
                {
                    x = originalAvPosition.x - newAvPosition.x + originalPsPosition.x,
                    y = originalPsPosition.y,
                    z = originalAvPosition.z - newAvPosition.z + originalPsPosition.z,
                };
                targetPs.position = newPsPosition;
            }
            
            ScaleGoesBrrMod.FixAvatarRootFlyingOff(RootFix);
        }
    }
}