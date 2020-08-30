using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC.UserCamera;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "1.0.0", "knah", "https://github.com/knah/VRCMods")]

namespace CameraMinus
{
    public class CameraMinusMod : MelonMod
    {
        private bool LensShown = true;
        private Text ShowButtonText = null;
        
        public override void OnApplicationStart()
        {
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, "Hide camera lens", ToggleLens, SetToggleLensButton);
            
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, "Zoom in", ZoomIn);
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, "Zoom out", ZoomOut);
            
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, "Enlarge camera", Enlarge);
            ExpansionKitApi.RegisterSimpleMenuButton(ExpandedMenu.CameraQuickMenu, "Shrink camera", Shrink);
        }

        private void Enlarge()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            var oldPosition = cameraController.photoCamera.transform.position;
            cameraController.transform.localScale *= 1.5f;
            cameraController.photoCamera.transform.position = oldPosition;
        }
        
        private void Shrink()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            var oldPosition = cameraController.photoCamera.transform.position;
            cameraController.transform.localScale /= 1.5f;
            cameraController.photoCamera.transform.position = oldPosition;
        }

        private void SetToggleLensButton(GameObject obj)
        {
            ShowButtonText = obj.GetComponentInChildren<Text>();

            ShowButtonText.text = LensShown ? "Hide camera lens" : "Show camera lens";
        }

        private void ToggleLens()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            
            LensShown = !LensShown;
            ShowButtonText.text = LensShown ? "Hide camera lens" : "Show camera lens";

            var lensMesh = cameraController.photoCamera.transform.Find("camera_lens_mesh");
            lensMesh.gameObject.SetActive(LensShown);
        }

        private void ZoomIn()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView > 10)
                    camera.fieldOfView -= 10;
        }
        
        private void ZoomOut()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView < 170)
                    camera.fieldOfView += 10;
        }
    }
}