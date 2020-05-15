using System;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MirrorResolutionUnlimiter;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;

[assembly:MelonModInfo(typeof(MirrorResolutionUnlimiterMod), "MirrorResolutionUnlimiter", "1.0", "knah")]
[assembly:MelonModGame("VRChat", "VRChat")]

namespace MirrorResolutionUnlimiter
{
    public class MirrorResolutionUnlimiterMod : MelonMod
    {
        private const string ModCategory = "MirrorResolutionUnlimiter";
        
        private const string MaxResPref = "MaxEyeTextureResolution";
        private const string MirrorMsaaPref = "MirrorMsaa";
        private const string AllMirrorsAutoPref = "AllMirrorsUseAutoRes";

        private static int ourMaxEyeResolution = 2048;
        private static bool ourAllMirrorsAuto = false;
        private static int ourMirrorMsaa = 0;

        public override void OnApplicationStart()
        {
            ModPrefs.RegisterCategory(ModCategory, "Mirror Resolution");
            ModPrefs.RegisterPrefInt(ModCategory, MaxResPref, 4096, "Max eye texture size");
            ModPrefs.RegisterPrefInt(ModCategory, MirrorMsaaPref, 0, "Mirror MSAA (0=default)");
            ModPrefs.RegisterPrefBool(ModCategory, AllMirrorsAutoPref, false, "Force auto resolution");
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(VRC_MirrorReflection).GetMethod(nameof(VRC_MirrorReflection.GetReflectionData))).GetValue(null);
                Imports.Hook((IntPtr) (&originalMethodPointer), typeof(MirrorResolutionUnlimiterMod).GetMethod(nameof(GetReflectionData), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
            }
            
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((_, __) => OnModSettingsApplied()));
            
            OnModSettingsApplied();
        }

        public override void OnModSettingsApplied()
        {
            ourMaxEyeResolution = ModPrefs.GetInt(ModCategory, MaxResPref);
            ourAllMirrorsAuto = ModPrefs.GetBool(ModCategory, AllMirrorsAutoPref);
            ourMirrorMsaa = ModPrefs.GetInt(ModCategory, MirrorMsaaPref);
            if (ourMirrorMsaa != 1 && ourMirrorMsaa != 2 && ourMirrorMsaa != 4 && ourMirrorMsaa != 8)
                ourMirrorMsaa = 0;
        }

        // this is slightly rewritten code from VRCSDK
        // if only the game was still in IL so that transpiler harmony patches worked
        private static IntPtr GetReflectionData(IntPtr thisPtr, IntPtr currentCameraPtr)
        {
            var @this = new VRC_MirrorReflection(thisPtr);
            var currentCamera = new Camera(currentCameraPtr);

            var reflections = @this._mReflections ?? (@this._mReflections = new Dictionary<Camera, VRC_MirrorReflection.ReflectionData>());

            // TryGetValue crashes in unhollower 0.4
            var reflectionData = reflections.ContainsKey(currentCamera)
                ? reflections[currentCamera]
                : reflections[currentCamera] = new VRC_MirrorReflection.ReflectionData {propertyBlock = new MaterialPropertyBlock()};

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

            int antiAliasing = ourMirrorMsaa == 0 ? Mathf.Clamp(1, QualitySettings.antiAliasing, (int) @this.maximumAntialiasing) : ourMirrorMsaa;
            @this._temporaryRenderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, antiAliasing);
            reflectionData.texture[0] = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 1);
            reflectionData.propertyBlock.SetTexture(VRC_MirrorReflection._texturePropertyId[0], reflectionData.texture[0]);
            if (currentCamera.stereoEnabled)
            {
                reflectionData.texture[1] = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, 1);
                reflectionData.propertyBlock.SetTexture(VRC_MirrorReflection._texturePropertyId[1], reflectionData.texture[1]);
            }

            return reflectionData.Pointer;
        }
    }
}