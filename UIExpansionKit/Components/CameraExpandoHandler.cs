using System;
using UnityEngine;

namespace UIExpansionKit.Components
{
    public class CameraExpandoHandler : MonoBehaviour
    {
        private Transform myTransform;
        public Transform PlayerCamera;
        public Transform CameraTransform;
        public float myScaleableDistance = 0.185f;

        public CameraExpandoHandler(IntPtr ptr) : base(ptr)
        {
        }

        private void Awake()
        {
            myTransform = transform;
        }

        private void Update()
        {
            var playerCameraUp = Vector3.up;
            myTransform.rotation = Quaternion.LookRotation(myTransform.position - PlayerCamera.position, playerCameraUp);
            myTransform.position = CameraTransform.position - CameraTransform.lossyScale.x * myScaleableDistance * playerCameraUp;
        }
    }
}