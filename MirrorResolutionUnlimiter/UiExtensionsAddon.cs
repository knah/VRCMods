using System.Runtime.CompilerServices;
using UIExpansionKit.API;
using UnityEngine;
using VRC.SDKBase;

namespace MirrorResolutionUnlimiter
{
    public static class UiExtensionsAddon
    {
        private static int PlayerLayer = 1 << 9; // todo: ask unity for these
        private static int PlayerLocalLayer = 1 << 10;
        private static int UiLayer = 1 << 5;
        private static int UiMenuLayer = 1 << 12;
        private static int UiInternalLayer = 1 << 19;
        private static int MirrorReflectionLayer = 1 << 18;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Optimize mirrors", OptimizeMirrors);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Beautify mirrors", BeautifyMirrors);

            ExpansionKitApi.RegisterSettingAsStringEnum(MirrorResolutionUnlimiterMod.ModCategory,
                MirrorResolutionUnlimiterMod.PixelLightsSetting,
                new[] {("default", "World default"), ("allow", "Force allow"), ("disable", "Force disable")});
        }

        private static void BeautifyMirrors()
        {
            foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                if (vrcMirrorReflection.isActiveAndEnabled)
                    if (MirrorResolutionUnlimiterMod.UiInMirrors.Value)
                        vrcMirrorReflection.m_ReflectLayers = -1 & ~PlayerLocalLayer;
                    else
                        vrcMirrorReflection.m_ReflectLayers =
                            -1 & ~UiLayer & ~UiMenuLayer & ~PlayerLocalLayer & ~UiInternalLayer;

        }

        private static void OptimizeMirrors()
        {
            foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                if (vrcMirrorReflection.isActiveAndEnabled)
                    vrcMirrorReflection.m_ReflectLayers = PlayerLayer | MirrorReflectionLayer | (MirrorResolutionUnlimiterMod.UiInMirrors.Value ? UiMenuLayer | UiInternalLayer | UiLayer : 0);
        }
    }
}