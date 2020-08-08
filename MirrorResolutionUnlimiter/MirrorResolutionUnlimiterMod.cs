using System;
using System.Linq;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Reflection;
using MelonLoader;
using MirrorResolutionUnlimiter;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.SDKBase;

[assembly:MelonModInfo(typeof(MirrorResolutionUnlimiterMod), "MirrorResolutionUnlimiter", "1.0.1", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonModGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

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
            MelonPrefs.RegisterCategory(ModCategory, "Mirror Resolution");
            MelonPrefs.RegisterInt(ModCategory, MaxResPref, 4096, "Max eye texture size");
            MelonPrefs.RegisterInt(ModCategory, MirrorMsaaPref, 0, "Mirror MSAA (0=default)");
            MelonPrefs.RegisterBool(ModCategory, AllMirrorsAutoPref, false, "Force auto resolution");
            
            unsafe
            {
                var methodInfo = Il2CppType.Of<VRC_MirrorReflection>().GetMethod(nameof(VRC_MirrorReflection.GetReflectionData), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var originalMethodPointer = *(IntPtr*) IL2CPP.il2cpp_method_get_from_reflection(methodInfo.Pointer);
                CompatHook((IntPtr) (&originalMethodPointer), typeof(MirrorResolutionUnlimiterMod).GetMethod(nameof(GetReflectionData), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
            }

            OnModSettingsApplied();

            if (AppDomain.CurrentDomain.GetAssemblies().Any(it => it.GetName().Name.StartsWith("UIExpansionKit")))
            {
                MelonLogger.Log("Adding UIExpansionKit buttons");
                typeof(UiExtensionsAddon)
                    .GetMethod(nameof(UiExtensionsAddon.Init),
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
                    .Invoke(null, new object[0]);
            }
        }

        private static void CompatHook(IntPtr first, IntPtr second)
        {
            typeof(Imports).GetMethod("Hook", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null, new object[] {first, second});
        }

        public override void OnModSettingsApplied()
        {
            ourMaxEyeResolution = MelonPrefs.GetInt(ModCategory, MaxResPref);
            ourAllMirrorsAuto = MelonPrefs.GetBool(ModCategory, AllMirrorsAutoPref);
            ourMirrorMsaa = MelonPrefs.GetInt(ModCategory, MirrorMsaaPref);
            if (ourMirrorMsaa != 1 && ourMirrorMsaa != 2 && ourMirrorMsaa != 4 && ourMirrorMsaa != 8)
                ourMirrorMsaa = 0;
        }

        // this is slightly rewritten code from VRCSDK
        // if only the game was still in IL so that transpiler harmony patches worked
        private static IntPtr GetReflectionData(IntPtr thisPtr, IntPtr currentCameraPtr)
        {
            try
            {
                var @this = new VRC_MirrorReflection(thisPtr);
                var currentCamera = new Camera(currentCameraPtr);

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

                int antiAliasing = ourMirrorMsaa == 0
                    ? Mathf.Clamp(1, QualitySettings.antiAliasing, (int) @this.maximumAntialiasing)
                    : ourMirrorMsaa;
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
            catch (Exception ex)
            {
                MelonLogger.LogError("Exception happened in GetReflectionData override; crash will likely follow: " + ex);
                return IntPtr.Zero;
            }
        }
    }
}