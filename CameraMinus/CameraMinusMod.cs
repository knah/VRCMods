using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using VRC.SDKBase;
using VRC.UserCamera;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "2.0.1", "knah", "https://github.com/knah/VRCMods")]

namespace CameraMinus
{
    internal partial class CameraMinusMod : MelonMod
    {
        private MelonPreferences_Entry<bool> myUseCameraExpando;
        private MelonPreferences_Entry<bool> myUnlimitCameraPickupDistance;

        public override void OnApplicationStart()
        {
            var category = MelonPreferences.CreateCategory("CameraMinus", "CameraMinus");
            myUseCameraExpando = category.CreateEntry("UseCameraExpando", true, "Use Camera expando (instead of QM expando)");
            myUnlimitCameraPickupDistance = category.CreateEntry("UnlimitCameraPickupDistance", true, "Longer camera pickup distance");

            ExpansionKitApi.GetSettingsCategory("CameraMinus")
                .AddLabel("Disable and enable camera to update camera expando visibility");
            
            GameObject cameraButton = null;
            GameObject qmButton = null;
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("CameraMinus", ShowCustomMenu, go =>
            {
                cameraButton = go;
                cameraButton.SetActive(myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("CameraMinus", ShowCustomMenu, go =>
            {
                qmButton = go;
                qmButton.SetActive(!myUseCameraExpando.Value);
            });

            myUseCameraExpando.OnValueChanged += (_, value) =>
            {
                if (cameraButton != null) cameraButton.SetActive(value);
                if (qmButton != null) qmButton.SetActive(!value);
            };

            myUnlimitCameraPickupDistance.OnValueChanged += (_, value) =>
            {
                UpdateCameraPickupDistance(value);
            };

            ExpansionKitApi.OnUiManagerInit += () =>
            {
                UpdateCameraPickupDistance(myUnlimitCameraPickupDistance.Value);
            };
        }

        private static void UpdateCameraPickupDistance(bool value)
        {
            var controller = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (controller != null)
                controller.transform.Find("ViewFinder").GetComponent<VRC_Pickup>().proximity = value ? 20 : 1;
        }

        private void ShowCustomMenu()
        {
            var customMenu = myUseCameraExpando.Value
                ? ExpansionKitApi.CreateCustomCameraExpandoPage(LayoutDescription.QuickMenu3Columns)
                : ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu3Columns);
            
            customMenu.AddToggleButton("Camera lens visible", ToggleLens, GetLensState);
            customMenu.AddSimpleButton("Enlarge camera", Enlarge);
            customMenu.AddSimpleButton("Shrink camera", Shrink);

            customMenu.AddSimpleButton("Reset zoom", ZoomReset);
            customMenu.AddSimpleButton("Zoom in", ZoomIn);
            customMenu.AddSimpleButton("Zoom out", ZoomOut);

            customMenu.AddSpacer();
            customMenu.AddSpacer();
            customMenu.AddSimpleButton("Back", () => customMenu.Hide());
            
            customMenu.Show();
        }

        private void Enlarge()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.transform.Find("ViewFinder").localScale *= 1.5f;
        }
        
        private void Shrink()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.transform.Find("ViewFinder").localScale /= 1.5f;
        }

        private void ToggleLens(bool enabled)
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;

            var lensMesh = cameraController.transform.Find("PhotoCamera/camera_lens_mesh");
            lensMesh.gameObject.SetActive(enabled);
        }

        private bool GetLensState()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return true;
            
            var lensMesh = cameraController.transform.Find("PhotoCamera/camera_lens_mesh");
            return lensMesh.gameObject.activeSelf;
        }

        private void ZoomIn()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView > 10)
                    camera.fieldOfView -= 10;
                else if (camera.fieldOfView > 1)
                    camera.fieldOfView -= 1;
        }

        private void ZoomOut()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            foreach (var camera in cameraController.GetComponentsInChildren<Camera>())
                if (camera.fieldOfView < 10)
                    camera.fieldOfView += 1;
                else if (camera.fieldOfView < 170)
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