using MelonLoader;
using UnityEngine;

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
            MelonPrefs.RegisterCategory(CategoryName, "Graphics settings");
            
            MelonPrefs.RegisterInt(CategoryName, MsaaLevel, -1, "MSAA Level (1/2/4/8)");
            MelonPrefs.RegisterBool(CategoryName, AllowMsaa, true, "Enable MSAA");
            MelonPrefs.RegisterBool(CategoryName, AnisoFilter, true, "Enable anisotropic filtering");
            MelonPrefs.RegisterBool(CategoryName, RealtimeShadows, true, "Realtime shadows");
            MelonPrefs.RegisterBool(CategoryName, SoftShadows, true, "Soft shadows");
            MelonPrefs.RegisterInt(CategoryName, PixelLights, -1, "Max pixel lights");
            MelonPrefs.RegisterInt(CategoryName, TextureLimit, -1, "Texture decimation");
            MelonPrefs.RegisterInt(CategoryName, GraphicsTier, -1, "Graphics tier (1/2/3)");
        }

        public static bool AllowMSAA => MelonPrefs.GetBool(CategoryName, AllowMsaa);
        public static int MSAALevel => MelonPrefs.GetInt(CategoryName, MsaaLevel);
        public static bool AllowAniso => MelonPrefs.GetBool(CategoryName, AnisoFilter);

        public static ShadowQuality ShadowQuality => MelonPrefs.GetBool(CategoryName, RealtimeShadows)
            ? (MelonPrefs.GetBool(CategoryName, SoftShadows) ? ShadowQuality.All : ShadowQuality.HardOnly)
            : ShadowQuality.Disable;

        public static int PixelLightCount => MelonPrefs.GetInt(CategoryName, PixelLights);
        public static int TextureSizeLimit => MelonPrefs.GetInt(CategoryName, TextureLimit);
        public static int HardwareGraphicsTier => MelonPrefs.GetInt(CategoryName, GraphicsTier);
        
    }
}