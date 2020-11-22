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
        private static int MirrorReflectionLayer = 1 << 18;
        
        public static void Init()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Optimize mirrors", OptimizeMirrors);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Beautify mirrors", BeautifyMirrors);
        }

        private static void BeautifyMirrors()
        {
            foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                if (vrcMirrorReflection.isActiveAndEnabled)
                    vrcMirrorReflection.m_ReflectLayers = -1 & ~UiLayer & ~UiMenuLayer & ~PlayerLocalLayer;
        }

        private static void OptimizeMirrors()
        {
            foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                if (vrcMirrorReflection.isActiveAndEnabled)
                    vrcMirrorReflection.m_ReflectLayers = PlayerLayer | MirrorReflectionLayer;
        }
    }
}