using MelonLoader;
using RuntimeGraphicsSettings;
using UnityEngine;
using UnityEngine.Rendering;

[assembly:MelonModInfo(typeof(RuntimeGraphicsSettingsMod), "Runtime Graphics Settings", RuntimeGraphicsSettingsMod.ModVersion, "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonModGame] // universal
namespace RuntimeGraphicsSettings
{
    public class RuntimeGraphicsSettingsMod : MelonMod
    {
        public const string ModVersion = "0.2.0";
        
        public override void OnApplicationStart()
        {
            RuntimeGraphicsSettings.RegisterSettings();
            DoApplySettings();
        }

        public override void OnModSettingsApplied()
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
                ? AnisotropicFiltering.ForceEnable
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