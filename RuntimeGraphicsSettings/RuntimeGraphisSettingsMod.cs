using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using VRCModLoader;

namespace RuntimeGraphicsSettings
{
    [VRCModInfo("Runtime Graphics Settings", ModVersion, "knah")]
    public class RuntimeGraphicsSettingsMod : VRCMod
    {
        public const string ModVersion = "0.1.0";
        
        [UsedImplicitly]
        public void OnApplicationStart()
        {
            RuntimeGraphicsSettings.RegisterSettings();
            DoApplySettings();
        }

        [UsedImplicitly]
        public void OnModSettingsApplied()
        {
            DoApplySettings();
        }

        void DoApplySettings()
        {
            if (RuntimeGraphicsSettings.AllowMSAA)
            {
                if (RuntimeGraphicsSettings.MSAALevel > 0)
                    QualitySettings.antiAliasing = RuntimeGraphicsSettings.MSAALevel;
            }
            else
                QualitySettings.antiAliasing = 1;

            QualitySettings.anisotropicFiltering = RuntimeGraphicsSettings.AllowAniso
                ? AnisotropicFiltering.Enable
                : AnisotropicFiltering.Disable;

            if (RuntimeGraphicsSettings.TextureSizeLimit >= 0)
                QualitySettings.masterTextureLimit = RuntimeGraphicsSettings.TextureSizeLimit;

            QualitySettings.shadows = RuntimeGraphicsSettings.ShadowQuality;

            if (RuntimeGraphicsSettings.PixelLightCount >= 0)
                QualitySettings.pixelLightCount = RuntimeGraphicsSettings.PixelLightCount;

            if (RuntimeGraphicsSettings.HardwareGraphicsTier > 0)
                Graphics.activeTier = (GraphicsTier) (RuntimeGraphicsSettings.HardwareGraphicsTier - 1);
        }
    }
}