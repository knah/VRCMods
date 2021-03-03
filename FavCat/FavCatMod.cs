using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FavCat;
using FavCat.CustomLists;
using FavCat.Database;
using FavCat.Modules;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Networking;
using VRC.Core;
using Object = UnityEngine.Object;
using ImageDownloaderClosure = ImageDownloader.__c__DisplayClass11_1;

[assembly:MelonInfo(typeof(FavCatMod), "FavCat", "1.0.9", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace FavCat
{
    public class FavCatMod : MelonMod
    {
        public static LocalStoreDatabase? Database;
        internal static FavCatMod Instance;

        internal AvatarModule? AvatarModule;
        private WorldsModule? myWorldsModule;
        private PlayersModule? myPlayerModule;
        
        private static bool ourInitDone;
        
        private static readonly ConcurrentQueue<Action> ToMainThreadQueue = new ConcurrentQueue<Action>();

        public override void OnApplicationStart()
        {
            Instance = this;

            Directory.CreateDirectory("./UserData/FavCatImport");
            
            ClassInjector.RegisterTypeInIl2Cpp<CustomPickerList>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomPicker>();
            
            ApiSnifferPatch.DoPatch();
            
            FavCatSettings.RegisterSettings();
            
            MelonLogger.Log("Creating database");
            Database = new LocalStoreDatabase(FavCatSettings.DatabasePath, FavCatSettings.ImageCachePath);
            
            Database.ImageHandler.TrimCache(FavCatSettings.MaxCacheSizeBytes).NoAwait();

            ExpansionKitApi.RegisterWaitConditionBeforeDecorating(WaitForInitDone());
        }

        internal CustomPickerList CreateCustomList(Transform parent)
        {
            var go = Object.Instantiate(AssetsHandler.ListPrefab, parent);
            go.SetActive(true);
            return go.AddComponent<CustomPickerList>();
        }

        public override void OnApplicationQuit()
        {
            Database?.Dispose();
            Database = null;
        }

        private IEnumerator WaitForInitDone()
        {
            while (!ourInitDone)
                yield return null;
        }

        public override void VRChat_OnUiManagerInit()
        {
            AssetsHandler.Load();

            try
            {
                if (FavCatSettings.IsEnableAvatarFavs)
                    AvatarModule = new AvatarModule();
            }
            catch (Exception ex)
            {
                MelonLogger.LogError($"Exception in avatar module init: {ex}");
            }

            try
            {
                if (FavCatSettings.IsEnableWorldFavs)
                    myWorldsModule = new WorldsModule();
            }
            catch (Exception ex)
            {
                MelonLogger.LogError($"Exception in world module init: {ex}");
            }
            
            try
            {
                if (FavCatSettings.IsEnablePlayerFavs)
                    myPlayerModule = new PlayersModule();
            }
            catch (Exception ex)
            {
                MelonLogger.LogError($"Exception in player module init: {ex}");
            }

            MelonLogger.Log("Initialized!");
            ourInitDone = true;
        }

        public override void OnUpdate()
        {
            AvatarModule?.Update();
            myWorldsModule?.Update();
            myPlayerModule?.Update();
            GlobalImageCache.OnUpdate();

            if (ToMainThreadQueue.TryDequeue(out var action))
                action();
        }

        public static MainThreadAwaitable YieldToMainThread()
        {
            return new MainThreadAwaitable();
        }

        public struct MainThreadAwaitable : INotifyCompletion
        {
            public bool IsCompleted => false;

            public MainThreadAwaitable GetAwaiter() => this;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                ToMainThreadQueue.Enqueue(continuation);
            }
        }
    }

    public class ApiSnifferPatch
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ApiPopulateDelegate(IntPtr @this, IntPtr dictionary, IntPtr someRef, IntPtr methodRef);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ImageDownloaderOnDoneDelegate(IntPtr thisPtr, IntPtr asyncOperationPtr);

        private static ApiPopulateDelegate ourOriginalApiPopulate = (@this, dictionary, @ref, @ref1) => false;
        private static ImageDownloaderOnDoneDelegate ourOriginalOnDone = (ptr, operationPtr) => { };

        private static readonly Type ImageDownloaderClosureType;
        private static readonly MethodInfo WebRequestField;
        private static readonly MethodInfo ImageUrlField;
        private static readonly MethodInfo? NestedClosureField;

        static ApiSnifferPatch()
        {
            ImageDownloaderClosureType = typeof(ImageDownloader).GetNestedTypes().Single(it => it.GetMethod(nameof(ImageDownloaderClosure._DownloadImageInternal_b__0)) != null);
            WebRequestField = ImageDownloaderClosureType.GetProperties().Single(it => it.PropertyType == typeof(UnityWebRequest)).GetMethod;
            NestedClosureField = ImageDownloaderClosureType.GetProperties().SingleOrDefault(it => it.PropertyType.IsNested && it.PropertyType.DeclaringType == typeof(ImageDownloader))?.GetMethod;
            Type? possibleNestedClosureType = NestedClosureField?.ReturnType;
            ImageUrlField = (NestedClosureField != null ? possibleNestedClosureType : ImageDownloaderClosureType)!.GetProperty("imageUrl")!.GetMethod;
        }


        public static void DoPatch()
        {
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(
                        typeof(ApiModel).GetMethods().Single(it =>
                            it.Name == nameof(ApiModel.SetApiFieldsFromJson) && it.GetParameters().Length == 2))
                    .GetValue(null);
                Imports.Hook((IntPtr) (&originalMethodPointer), typeof(ApiSnifferPatch).GetMethod(nameof(ApiSnifferStatic))!.MethodHandle.GetFunctionPointer());
                ourOriginalApiPopulate = Marshal.GetDelegateForFunctionPointer<ApiPopulateDelegate>(originalMethodPointer);
            }

            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(ImageDownloaderClosureType.GetMethod(nameof(ImageDownloaderClosure._DownloadImageInternal_b__0))).GetValue(null);
                Imports.Hook((IntPtr) (&originalMethodPointer), typeof(ApiSnifferPatch).GetMethod(nameof(ImageSnifferPatch))!.MethodHandle.GetFunctionPointer());
                ourOriginalOnDone = Marshal.GetDelegateForFunctionPointer<ImageDownloaderOnDoneDelegate>(originalMethodPointer);
            }
        }

        private static readonly object[] EmptyObjectArray = new object[0];

        public static void ImageSnifferPatch(IntPtr instancePtr, IntPtr asyncOperationPtr)
        {
            ourOriginalOnDone(instancePtr, asyncOperationPtr);

            try
            {
                if (!FavCatSettings.UseLocalImageCache || FavCatMod.Database == null)
                    return;

                var closure = Activator.CreateInstance(ImageDownloaderClosureType, instancePtr);
                var url = (string) ImageUrlField.Invoke(NestedClosureField?.Invoke(closure, EmptyObjectArray) ?? closure, EmptyObjectArray);
                
                var webRequest = (UnityWebRequest) WebRequestField.Invoke(closure, EmptyObjectArray);
                if (webRequest.isNetworkError || webRequest.isHttpError)
                    return;

                if (webRequest.downloadedBytes > 1024 * 1024)
                {
                    if (Imports.IsDebugMode())
                        MelonLogger.Log($"Ignored downloaded image from {url} because it's bigger than 1 MB");
                    return; // ignore images over 1 megabyte, 256-pixel previews should not be that big
                }

                FavCatMod.Database.ImageHandler.StoreImageAsync(url, webRequest.downloadHandler.data).NoAwait();
            }
            catch (Exception ex)
            {
                MelonLogger.LogError($"Exception in image downloader patch: {ex}");
            }
        }

        public static bool ApiSnifferStatic(IntPtr @this, IntPtr dictionary, IntPtr someRef, IntPtr methodInfo)
        {
            var result = ourOriginalApiPopulate(@this, dictionary, someRef, methodInfo);

            try
            {
                var apiModel = new ApiModel(@this);
                if (!apiModel.Populated) return result;

                var maybeUser = apiModel.TryCast<APIUser>();
                if (maybeUser != null) FavCatMod.Database?.UpdateStoredPlayer(maybeUser);
                var maybeAvatar = apiModel.TryCast<ApiAvatar>();
                if (maybeAvatar != null) FavCatMod.Database?.UpdateStoredAvatar(maybeAvatar);
                var maybeWorld = apiModel.TryCast<ApiWorld>();
                if (maybeWorld != null) FavCatMod.Database?.UpdateStoredWorld(maybeWorld);
            }
            catch (Exception ex)
            {
                MelonLogger.LogError($"Exception in API sniffer patch: {ex}");
            }

            return result;
        }
    }
}
