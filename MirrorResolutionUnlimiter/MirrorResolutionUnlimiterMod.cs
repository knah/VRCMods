using System;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MirrorResolutionUnlimiter;
using UnhollowerBaseLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;
using Object = UnityEngine.Object;

[assembly:MelonModInfo(typeof(MirrorResolutionUnlimiterMod), "MirrorResolutionUnlimiter", "1.0", "knah")]
[assembly:MelonModGame("VRChat", "VRChat")]

namespace MirrorResolutionUnlimiter
{
    public class MirrorResolutionUnlimiterMod : MelonMod
    {
        private const string ModCategory = "MirrorResolutionUnlimiter";
        
        private const string MaxResPref = "MaxEyeTextureResolution";
        private const string AllMirrorsAutoPref = "AllMirrorsUseAutoRes";

        public override void OnApplicationStart()
        {
            ModPrefs.RegisterCategory(ModCategory, "Mirror Resolution");
            ModPrefs.RegisterPrefInt(ModCategory, MaxResPref, 4096, "Max eye texture size");
            ModPrefs.RegisterPrefBool(ModCategory, AllMirrorsAutoPref, false, "Force auto resolution");
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(VRC_MirrorReflection).GetMethod(nameof(VRC_MirrorReflection.GetReflectionData))).GetValue(null);
                Imports.Hook((IntPtr) (&originalMethodPointer), typeof(MirrorResolutionUnlimiterMod).GetMethod(nameof(GetReflectionData), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
            }
            
            SceneManager.add_sceneLoaded(new Action<Scene, LoadSceneMode>((_, __) => OnModSettingsApplied()));
        }

        public override void OnModSettingsApplied()
        {
            if (!ModPrefs.GetBool(ModCategory, AllMirrorsAutoPref)) return;
            
            foreach (var vrcMirrorReflection in Object.FindObjectsOfType<VRC_MirrorReflection>())
                vrcMirrorReflection.mirrorResolution = VRC_MirrorReflection.Dimension.Auto;
        }

        private static IntPtr GetReflectionData(IntPtr thisPtr, IntPtr currentCameraPtr)
        {
            var @this = new VRC_MirrorReflection(thisPtr);
            var currentCamera = new Camera(currentCameraPtr);

            VRC_MirrorReflection.ReflectionData reflectionData;
            if (@this._mReflections == null)
                @this._mReflections = new Dictionary<Camera, VRC_MirrorReflection.ReflectionData>();

            if (!@this._mReflections.ContainsKey(currentCamera))
            {
                reflectionData = new VRC_MirrorReflection.ReflectionData()
                {
                    propertyBlock = new MaterialPropertyBlock()
                };
                @this._mReflections[currentCamera] = reflectionData;
            }
            else
            {
                reflectionData = @this._mReflections[currentCamera];
            }
            
            int width;
            int height;
            var maxEyeSize = ModPrefs.GetInt(ModCategory, MaxResPref);

            if (@this.mirrorResolution == VRC_MirrorReflection.Dimension.Auto || ModPrefs.GetBool(ModCategory, AllMirrorsAutoPref))
            {
                width = Mathf.Min(currentCamera.pixelWidth, maxEyeSize);
                height = Mathf.Min(currentCamera.pixelHeight, maxEyeSize);
            }
            else
            {
                width = (int) @this.mirrorResolution;
                height = (int) @this.mirrorResolution;
            }

            int antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
            for (int index = 0; index < 2 && (index <= 0 || currentCamera.stereoEnabled); ++index)
            {
                if (reflectionData.texture[index])
                    RenderTexture.ReleaseTemporary(reflectionData.texture[index]);
                reflectionData.texture[index] = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, antiAliasing);
                reflectionData.propertyBlock.SetTexture(VRC_MirrorReflection._texturePropertyId[index], reflectionData.texture[index]);
            }

            return reflectionData.Pointer;
        }
    }
}