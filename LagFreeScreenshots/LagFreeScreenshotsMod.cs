using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Harmony;
using LagFreeScreenshots;
using MelonLoader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UIExpansionKit.API;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.UserCamera;
using Object = UnityEngine.Object;
using CameraUtil = ObjectPublicCaSiVeUnique;

[assembly:MelonInfo(typeof(LagFreeScreenshotsMod), "Lag Free Screenshots", "1.0.1", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace LagFreeScreenshots
{
    public class LagFreeScreenshotsMod : MelonMod
    {
        private static readonly AwaitProvider ourToMainThread = new AwaitProvider();
        private static readonly AwaitProvider ourToEndOfFrame = new AwaitProvider();

        private const string SettingsCategory = "LagFreeScreenshots";
        private const string SettingEnableMod = "Enabled";
        private const string SettingScreenshotFormat = "ScreenshotFormat";
        private const string SettingJpegPercent = "JpegPercent";

        public override void OnApplicationStart()
        {
            harmonyInstance.Patch(
                typeof(CameraUtil.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObBosareInAcre2StUnique).GetMethod("MoveNext"),
                new HarmonyMethod(AccessTools.Method(typeof(LagFreeScreenshotsMod), nameof(MoveNextPatch))));
            
            MelonPrefs.RegisterCategory(SettingsCategory, "Lag Free Screenshots");
            MelonPrefs.RegisterBool(SettingsCategory, SettingEnableMod, true, "Enabled");
            MelonPrefs.RegisterString(SettingsCategory, SettingScreenshotFormat, "png", "Screenshot format");
            MelonPrefs.RegisterInt(SettingsCategory, SettingJpegPercent, 95, "JPEG quality (0-100)");
            
            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit" && !it.Info.Version.StartsWith("0.1."))) 
                AddEnumSettings();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddEnumSettings()
        {
            ExpansionKitApi.RegisterSettingAsStringEnum(SettingsCategory, SettingScreenshotFormat, new []{("png", "PNG"), ("jpeg", "JPEG (slow!)")});
        }

        public override void OnUpdate()
        {
            ourToMainThread.Flush();
        }

        public override void OnGUI()
        {
            ourToEndOfFrame.Flush();
        }

        public static bool MoveNextPatch(ref bool __result, CameraUtil.ObjectNPrivateSealedIEnumerator1ObjectIEnumeratorIDisposableInObBosareInAcre2StUnique __instance)
        {
            if (!MelonPrefs.GetBool(SettingsCategory, SettingEnableMod))
                return true;
            
            __result = false;
            TakeScreenshot(__instance.cam, __instance.resWidth,
                __instance.resHeight, __instance.alpha).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    MelonLogger.LogWarning($"Free-floating task failed with exception: {t.Exception}");
            });
            return false;
        }
        
        public static async Task TakeScreenshot(Camera camera, int w, int h, bool hasAlpha)
        {
            await ourToEndOfFrame.Yield();

            var renderTexture = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8);

            var oldCameraTarget = camera.targetTexture;
            var oldCameraFov = camera.fieldOfView;
            
            camera.targetTexture = renderTexture;
            
            camera.Render();

            camera.targetTexture = oldCameraTarget;
            camera.fieldOfView = oldCameraFov;

            byte[] data = null;
            var readbackSupported = SystemInfo.supportsAsyncGPUReadback;
            if (readbackSupported)
            {
                if (Imports.IsDebugMode())
                    MelonLogger.Log("Supports readback");
                
                var request = AsyncGPUReadback.Request(renderTexture, 0, hasAlpha ? TextureFormat.ARGB32 : TextureFormat.RGB24,new Action<AsyncGPUReadbackRequest>(r =>
                {
                    if (r.hasError)
                        MelonLogger.LogWarning("Readback request finished with error (w)");
                    
                    var sw = Stopwatch.StartNew();
                    data = ToBytes(r.GetDataRaw(0), r.GetLayerDataSize());
                    if (Imports.IsDebugMode())
                        MelonLogger.Log($"Bytes readback took {sw.ElapsedMilliseconds}");
                }));
                
                while (!request.done && !request.hasError && data == null)
                    await ourToMainThread.Yield();

                if (request.hasError)
                    MelonLogger.LogWarning("Readback request finished with error");
                
                if (data == null)
                {
                    if (Imports.IsDebugMode())
                        MelonLogger.Log("Data was null after request was done, waiting more");
                    await ourToMainThread.Yield();
                }
            }
            else
            {
                if (Imports.IsDebugMode())
                    MelonLogger.Log("Does not support readback");
                
                RenderTexture.active = renderTexture;
                var newTexture = new Texture2D(w, h, hasAlpha ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
                newTexture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                newTexture.Apply();
                RenderTexture.active = null;

                data = newTexture.GetRawTextureData();
                
                Object.Destroy(newTexture);
            }
            
            RenderTexture.ReleaseTemporary(renderTexture);

            var targetFile = GetPath(w, h);
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            
            await EncodeAndSavePicture(targetFile, data, w, h, hasAlpha).ConfigureAwait(false);
        }
        
        private static byte[] ToBytes(IntPtr pointer, int length)
        {
            var data = new byte[length];

            Marshal.Copy(pointer, data, 0, data.Length);

            return data;
        }

        private static async Task EncodeAndSavePicture(string filePath, byte[] pixels, int w, int h, bool hasAlpha)
        {
            // yield to background thread
            await Task.Delay(1).ConfigureAwait(false);

            using var image = hasAlpha ? (Image) Image.LoadPixelData<Argb32>(pixels, w, h) : Image.LoadPixelData<Rgb24>(pixels, w, h);
            
            image.Mutate(it => it.Flip(FlipMode.Vertical));

            IImageEncoder encoder;
            await ourToMainThread.Yield();
            if (MelonPrefs.GetString(SettingsCategory, SettingScreenshotFormat) == "jpeg")
            {
                encoder = new JpegEncoder() {Quality = MelonPrefs.GetInt(SettingsCategory, SettingJpegPercent)};
                filePath = Path.ChangeExtension(filePath, ".jpeg");
            }
            else
                encoder = new PngEncoder();

            await Task.Delay(1).ConfigureAwait(false);

            await image.SaveAsync(filePath, encoder).ConfigureAwait(false);

            await ourToMainThread.Yield();
            
            MelonLogger.Log($"Image saved to {filePath}");
            // compatibility with log-reading tools
            UnityEngine.Debug.Log($"Took screenshot to: {filePath}");
        }

        private static Func<int, int, string> ourOurGetPathMethod;
        
        static string GetPath(int w, int h)
        {
            ourOurGetPathMethod ??= (Func<int, int, string>) Delegate.CreateDelegate(typeof(Func<int, int, string>),
                typeof(CameraUtil)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single(it =>
                        it.Name.StartsWith("Method_Private_Static_String_Int32_Int32_") && XrefScanner.XrefScan(it)
                            .Any(jt => jt.Type == XrefType.Global &&
                                       "yyyy-MM-dd_HH-mm-ss.fff" == jt.ReadAsObject()?.ToString())));

            return ourOurGetPathMethod(w, h);
        }
    }
}