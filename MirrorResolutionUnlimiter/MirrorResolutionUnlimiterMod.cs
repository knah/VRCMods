using System;
using System.Linq;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MirrorResolutionUnlimiter;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.SDKBase;

[assembly:MelonInfo(typeof(MirrorResolutionUnlimiterMod), "MirrorResolutionUnlimiter", "1.1.4", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace MirrorResolutionUnlimiter
{
    internal partial class MirrorResolutionUnlimiterMod : MelonMod
    {
        internal const string ModCategory = "MirrorResolutionUnlimiter";
        
        private const string MaxResPref = "MaxEyeTextureResolution";
        private const string MirrorMsaaPref = "MirrorMsaa";
        private const string AllMirrorsAutoPref = "AllMirrorsUseAutoRes";
        internal const string PixelLightsSetting = "PixelLightsSettings";

        private static int ourMaxEyeResolution = 2048;
        private static bool ourAllMirrorsAuto = false;
        private static int ourMirrorMsaa = 0;
        private static MelonPreferences_Entry<bool> ourMsaaIsUpperLimit;
        private static MelonLogger.Instance ourLogger;
        internal static MelonPreferences_Entry<bool> UiInMirrors;

        private MelonPreferences_Entry<string> myPixelLightsSetting;

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<OriginalPixelLightsSettingKeeper>();

            ourLogger = LoggerInstance;

            var category = MelonPreferences.CreateCategory(ModCategory, "Mirror Resolution");
            var maxTextureRes = category.CreateEntry(MaxResPref, 4096, "Max eye texture size");
            maxTextureRes.OnValueChanged += (_, v) => ourMaxEyeResolution = v;
            ourMaxEyeResolution = maxTextureRes.Value;

            var mirrorMsaa = category.CreateEntry(MirrorMsaaPref, 0, "Mirror MSAA (0=default)");
            mirrorMsaa.OnValueChanged += (_, v) =>
            {
                ourMirrorMsaa = v;
                if (ourMirrorMsaa != 1 && ourMirrorMsaa != 2 && ourMirrorMsaa != 4 && ourMirrorMsaa != 8)
                    ourMirrorMsaa = 0;
            };
            ourMirrorMsaa = mirrorMsaa.Value;
            if (ourMirrorMsaa != 1 && ourMirrorMsaa != 2 && ourMirrorMsaa != 4 && ourMirrorMsaa != 8)
                ourMirrorMsaa = 0;

            var forceAutoRes = category.CreateEntry(AllMirrorsAutoPref, false, "Force auto resolution");
            forceAutoRes.OnValueChanged += (_, v) => ourAllMirrorsAuto = v;
            ourAllMirrorsAuto = forceAutoRes.Value;
            
            myPixelLightsSetting = category.CreateEntry(PixelLightsSetting, "default", "Pixel lights in mirrors");
            myPixelLightsSetting.OnValueChangedUntyped += UpdateMirrorPixelLights;

            ourMsaaIsUpperLimit = category.CreateEntry("MsaaIsUpperLimit", true, "Mirror MSAA setting is upper limit (otherwise static)");

            UiInMirrors = category.CreateEntry("UiInMirrors", false, "Include UI in mirrors when using Optimize/Beautify buttons");

            HarmonyInstance.Patch(
                AccessTools.Method(typeof(VRC_MirrorReflection), nameof(VRC_MirrorReflection.GetReflectionData)),
                prefix: new HarmonyMethod(typeof(MirrorResolutionUnlimiterMod), nameof(GetReflectionData)));

            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit"))
            {
                ourLogger.Msg("Adding UIExpansionKit buttons");
                UiExtensionsAddon.Init();
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex != -1) return;
            
            foreach (var mirror in Resources.FindObjectsOfTypeAll<VRC_MirrorReflection>())
            {
                var store = mirror.gameObject.GetComponent<OriginalPixelLightsSettingKeeper>() ??
                            mirror.gameObject.AddComponent<OriginalPixelLightsSettingKeeper>();
                store.OriginalValue = mirror.m_DisablePixelLights;
            }

            UpdateMirrorPixelLights();
        }

        private void UpdateMirrorPixelLights()
        {
            var allMirrors = Resources.FindObjectsOfTypeAll<VRC_MirrorReflection>();
            var currentMode = myPixelLightsSetting.Value;
            foreach (var mirror in allMirrors)
            {
                switch (currentMode)
                {
                    case "default":
                        mirror.m_DisablePixelLights = mirror.gameObject.GetComponent<OriginalPixelLightsSettingKeeper>()?.OriginalValue ?? mirror.m_DisablePixelLights;
                        break;
                    case "disable":
                        mirror.m_DisablePixelLights = true;
                        break;
                    case "allow":
                        mirror.m_DisablePixelLights = false;
                        break;
                }
            }
        }

        // this is slightly rewritten code from VRCSDK
        // if only the game was still in IL so that transpiler harmony patches worked
        private static bool GetReflectionData(VRC_MirrorReflection __instance, Camera __0, ref VRC_MirrorReflection.ReflectionData __result)
        {
            try
            {
                var @this = __instance;
                var currentCamera = __0;

                var reflections = @this._mReflections ??
                                  (@this._mReflections = new Dictionary<Camera, VRC_MirrorReflection.ReflectionData>());

                // TryGetValue crashes in unhollower 0.4
                var reflectionData = reflections.ContainsKey(currentCamera)
                    ? reflections[currentCamera]
                    : reflections[currentCamera] = new VRC_MirrorReflection.ReflectionData
                        {propertyBlock = new MaterialPropertyBlock()};

                if (@this._temporaryRenderTexture)
                    RenderTexture.ReleaseTemporary(@this._temporaryRenderTexture);
                if (reflectionData.texture[0])
                    RenderTexture.ReleaseTemporary(reflectionData.texture[0]);
                if (reflectionData.texture[1])
                    RenderTexture.ReleaseTemporary(reflectionData.texture[1]);

                int width;
                int height;

                if (ourAllMirrorsAuto || @this.mirrorResolution == VRC_MirrorReflection.Dimension.Auto)
                {
                    width = Mathf.Min(currentCamera.pixelWidth, ourMaxEyeResolution);
                    height = Mathf.Min(currentCamera.pixelHeight, ourMaxEyeResolution);
                }
                else
                    width = height = (int) @this.mirrorResolution;

                var requestedMsaa = currentCamera.targetTexture?.antiAliasing ?? QualitySettings.antiAliasing;
                if (ourMirrorMsaa != 0)
                {
                    requestedMsaa = ourMsaaIsUpperLimit.Value
                        ? Mathf.Clamp(requestedMsaa, 1, ourMirrorMsaa)
                        : ourMirrorMsaa;
                }
                else
                {
                    requestedMsaa = Mathf.Clamp(requestedMsaa, 1, (int)@this.maximumAntialiasing);
                }
                @this._temporaryRenderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, requestedMsaa);
                reflectionData.texture[0] = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 1);
                reflectionData.propertyBlock.SetTexture(VRC_MirrorReflection._texturePropertyId[0], reflectionData.texture[0]);
                if (currentCamera.stereoEnabled)
                {
                    reflectionData.texture[1] = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 1);
                    reflectionData.propertyBlock.SetTexture(VRC_MirrorReflection._texturePropertyId[1], reflectionData.texture[1]);
                }

                __result = reflectionData;
                return false;
            }
            catch (Exception ex)
            {
                ourLogger.Error("Exception happened in GetReflectionData override" + ex);
            }
            return true;
        }
    }
}