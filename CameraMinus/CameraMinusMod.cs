using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using VRC.SDKBase;
using VRC.UserCamera;

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "3.0.0", "knah", "https://github.com/knah/VRCMods")]

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
            
            GameObject cameraEnlargeButton = null;
            GameObject cameraShrinkButton = null;
            GameObject qmEnlargeButton = null;
            GameObject qmShrinkButton = null;
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Enlarge camera", Enlarge, go =>
            {
                cameraEnlargeButton = go;
                cameraEnlargeButton.SetActive(myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Shrink camera", Shrink, go =>
            {
                cameraShrinkButton = go;
                cameraShrinkButton.SetActive(myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Enlarge camera", Enlarge, go =>
            {
                qmEnlargeButton = go;
                qmEnlargeButton.SetActive(!myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Shrink camera", Enlarge, go =>
            {
                qmShrinkButton = go;
                qmShrinkButton.SetActive(!myUseCameraExpando.Value);
            });

            myUseCameraExpando.OnValueChanged += (_, value) =>
            {
                if (cameraEnlargeButton != null) cameraEnlargeButton.SetActive(value);
                if (cameraShrinkButton != null) cameraShrinkButton.SetActive(value);
                if (qmEnlargeButton != null) qmEnlargeButton.SetActive(!value);
                if (qmShrinkButton != null) qmShrinkButton.SetActive(!value);
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
    }
}