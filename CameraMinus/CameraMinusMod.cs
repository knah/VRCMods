using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using VRC.UserCamera;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "1.1.0", "knah", "https://github.com/knah/VRCMods")]

namespace CameraMinus
{
    public class CameraMinusMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            var customMenu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("CameraMinus", () => customMenu.Show());
            
            customMenu.AddToggleButton("Camera lens visible", ToggleLens, GetLensState);
            customMenu.AddSimpleButton("Enlarge camera", Enlarge);
            customMenu.AddSimpleButton("Shrink camera", Shrink);

            customMenu.AddSimpleButton("Reset zoom", ZoomReset);
            customMenu.AddSimpleButton("Zoom in", ZoomIn);
            customMenu.AddSimpleButton("Zoom out", ZoomOut);

            customMenu.AddSpacer();
            customMenu.AddSpacer();
            customMenu.AddSimpleButton("Back", () => customMenu.Hide());
        }

        private void Enlarge()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.viewFinder.transform.localScale *= 1.5f;
        }
        
        private void Shrink()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.viewFinder.transform.localScale /= 1.5f;
        }

        private void ToggleLens(bool enabled)
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;

            var lensMesh = cameraController.photoCamera.transform.Find("camera_lens_mesh");
            lensMesh.gameObject.SetActive(enabled);
        }

        private bool GetLensState()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return true;
            
            var lensMesh = cameraController.photoCamera.transform.Find("camera_lens_mesh");
            return lensMesh.gameObject.activeSelf;
        }

        private void ZoomIn()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView > 10)
                    camera.fieldOfView -= 10;
        }
        
        private void ZoomReset()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                camera.fieldOfView = 60;
        }
        
        private void ZoomOut()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView < 170)
                    camera.fieldOfView += 10;
        }

        private void ZoomReset()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                camera.fieldOfView = 60;
        }
    }
}