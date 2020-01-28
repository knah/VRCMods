using UnityEngine;
using VRCTools;

namespace RuntimeGraphicsSettings
{
    public static class RuntimeGraphicsSettings
    {
        private const string CategoryName = "GraphicsSettings";
        private const string MsaaLevel = "MSAALevel";
        private const string AllowMsaa = "AllowMSAA";
        private const string AnisoFilter = "AnisotropicFiltering";
        private const string RealtimeShadows = "RealtimeShadows";
        private const string SoftShadows = "SoftShadows";
        private const string PixelLights = "PixelLights";
        private const string TextureLimit = "MasterTextureLimit";
        private const string GraphicsTier = "GraphicsTier";

        public static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(CategoryName, "Graphics settings");
            
            ModPrefs.RegisterPrefInt(CategoryName, MsaaLevel, -1);
            ModPrefs.RegisterPrefBool(CategoryName, AllowMsaa, true);
            ModPrefs.RegisterPrefBool(CategoryName, AnisoFilter, true);
            ModPrefs.RegisterPrefBool(CategoryName, RealtimeShadows, true);
            ModPrefs.RegisterPrefBool(CategoryName, SoftShadows, true);
            ModPrefs.RegisterPrefInt(CategoryName, PixelLights, -1);
            ModPrefs.RegisterPrefInt(CategoryName, TextureLimit, -1, "Texture decimation");
            ModPrefs.RegisterPrefInt(CategoryName, GraphicsTier, -1);
        }

        public static bool AllowMSAA => ModPrefs.GetBool(CategoryName, AllowMsaa);
        public static int MSAALevel => ModPrefs.GetInt(CategoryName, MsaaLevel);
        public static bool AllowAniso => ModPrefs.GetBool(CategoryName, AnisoFilter);

        public static ShadowQuality ShadowQuality => ModPrefs.GetBool(CategoryName, RealtimeShadows)
            ? (ModPrefs.GetBool(CategoryName, SoftShadows) ? ShadowQuality.All : ShadowQuality.HardOnly)
            : ShadowQuality.Disable;

        public static int PixelLightCount => ModPrefs.GetInt(CategoryName, PixelLights);
        public static int TextureSizeLimit => ModPrefs.GetInt(CategoryName, TextureLimit);
        public static int HardwareGraphicsTier => ModPrefs.GetInt(CategoryName, GraphicsTier);
        
    }
}