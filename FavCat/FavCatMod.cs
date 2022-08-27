﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using FavCat;
using FavCat.CustomLists;
using FavCat.Database;
using FavCat.Modules;
using HarmonyLib;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Networking;
using VRC.Core;
using VRC.UI;
using ImageDownloaderClosure = ImageDownloader.__c__DisplayClass11_0;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(FavCatMod), "FavCat", "1.1.16", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace FavCat
{
    internal partial class FavCatMod : MelonMod
    {
        public static LocalStoreDatabase? Database;
        public static MelonLogger.Instance Logger;
        
        internal static FavCatMod Instance;

        internal AvatarModule? AvatarModule;
        internal WorldsModule? WorldsModule;
        internal PlayersModule? PlayerModule;

        internal static PageUserInfo PageUserInfo;
        
        public override void OnApplicationStart()
        {
            Instance = this;
            Logger = LoggerInstance;
            if (!CheckWasSuccessful || !MustStayTrue || MustStayFalse) return;

            Directory.CreateDirectory("./UserData/FavCatImport");
            
            ClassInjector.RegisterTypeInIl2Cpp<CustomPickerList>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomPicker>();
            
            ApiSnifferPatch.DoPatch();
            
            FavCatSettings.RegisterSettings();
            
            Logger.Msg("Creating database");
            Database = new LocalStoreDatabase(FavCatSettings.DatabasePath.Value, FavCatSettings.ImageCachePath.Value);
            
            Database.ImageHandler.TrimCache(FavCatSettings.MaxCacheSizeBytes).NoAwait();

            foreach (var methodInfo in typeof(AvatarPedestal).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).Where(it => it.Name.StartsWith("Method_Private_Void_ApiContainer_") && it.GetParameters().Length == 1))
            {
                HarmonyInstance.Patch(methodInfo, new HarmonyMethod(typeof(FavCatMod), nameof(AvatarPedestalPatch)));
            }
            
            DoAfterUiManagerInit(OnUiManagerInit);
        }

        private static void AvatarPedestalPatch(ApiContainer __0)
        {
            if (__0.Code != 200) return;
            var model = __0.Model?.TryCast<ApiAvatar>();
            if (model == null) return;
            
            if (MelonDebug.IsEnabled())
                MelonDebug.Msg($"Ingested avatar with ID={model.id}");
            Database?.UpdateStoredAvatar(model);
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

        public void OnUiManagerInit()
        {
            AssetsHandler.Load();

            try
            {
                if (FavCatSettings.EnableAvatarFavs.Value)
                    AvatarModule = new AvatarModule();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in avatar module init: {ex}");
            }

            try
            {
                if (FavCatSettings.EnableWorldFavs.Value)
                    WorldsModule = new WorldsModule();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in world module init: {ex}");
            }
            
            try
            {
                if (FavCatSettings.EnablePlayerFavs.Value)
                    PlayerModule = new PlayersModule();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in player module init: {ex}");
            }

            PageUserInfo = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo").GetComponent<PageUserInfo>();
            Logger.Msg("Initialized!");
        }

        public override void OnUpdate()
        {
            AvatarModule?.Update();
            WorldsModule?.Update();
            PlayerModule?.Update();
            GlobalImageCache.OnUpdate();
        }
    }

    public class ApiSnifferPatch
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte ApiPopulateDelegate(IntPtr @this, IntPtr dictionary, IntPtr someRef, IntPtr methodRef);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ImageDownloaderOnDoneDelegate(IntPtr thisPtr, IntPtr asyncOperationPtr, IntPtr methodInfo);

        private static ApiPopulateDelegate ourOriginalApiPopulate = (_, _, _, _) => 0;
        private static ApiPopulateDelegate ourOriginalApiPopulateTokens = (_, _, _, _) => 0;
        private static ImageDownloaderOnDoneDelegate ourOriginalOnDone = (_, _, _) => { };

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
            NativePatchUtils.NativePatch(typeof(ApiModel).GetMethods().Single(it =>
                    it.Name == nameof(ApiModel.SetApiFieldsFromJson) && it.GetParameters().Length == 2 && it.GetParameters()[0].ParameterType.GenericTypeArguments[1] == typeof(Il2CppSystem.Object)),
                out ourOriginalApiPopulate, ApiSnifferStatic);
            
            NativePatchUtils.NativePatch(typeof(ApiModel).GetMethods().Single(it =>
                    it.Name == nameof(ApiModel.SetApiFieldsFromJson) && it.GetParameters().Length == 2 && it.GetParameters()[0].ParameterType.GenericTypeArguments[1] != typeof(Il2CppSystem.Object)),
                out ourOriginalApiPopulateTokens, ApiSnifferStaticTokens);
            
            NativePatchUtils.NativePatch(ImageDownloaderClosureType.GetMethod(nameof(ImageDownloaderClosure
                ._DownloadImageInternal_b__0))!, out ourOriginalOnDone, ImageSnifferPatch);
        }

        private static readonly object[] EmptyObjectArray = new object[0];

        public static void ImageSnifferPatch(IntPtr instancePtr, IntPtr asyncOperationPtr, IntPtr methodInfo)
        {
            ourOriginalOnDone(instancePtr, asyncOperationPtr, methodInfo);

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
                    if (MelonDebug.IsEnabled())
                        MelonDebug.Msg($"Ignored downloaded image from {url} because it's bigger than 1 MB");
                    return; // ignore images over 1 megabyte, 256-pixel previews should not be that big
                }

                FavCatMod.Database.ImageHandler.StoreImageAsync(url, webRequest.downloadHandler.data).NoAwait();
            }
            catch (Exception ex)
            {
                FavCatMod.Logger.Error($"Exception in image downloader patch: {ex}");
            }
        }

        public static byte ApiSnifferStatic(IntPtr @this, IntPtr dictionary, IntPtr someRef, IntPtr methodInfo)
        {
            var result = ourOriginalApiPopulate(@this, dictionary, someRef, methodInfo);

            ApiSnifferBody(@this);

            return result;
        }
        
        public static byte ApiSnifferStaticTokens(IntPtr @this, IntPtr dictionary, IntPtr someRef, IntPtr methodInfo)
        {
            var result = ourOriginalApiPopulateTokens(@this, dictionary, someRef, methodInfo);

            ApiSnifferBody(@this);

            return result;
        }

        private static void ApiSnifferBody(IntPtr @this)
        {
            try
            {
                var apiModel = new ApiModel(@this);
                if (!apiModel.Populated) return;

                var maybeUser = apiModel.TryCast<APIUser>();
                if (maybeUser != null) FavCatMod.Database?.UpdateStoredPlayer(maybeUser);
                var maybeWorld = apiModel.TryCast<ApiWorld>();
                if (maybeWorld != null) FavCatMod.Database?.UpdateStoredWorld(maybeWorld);
            }
            catch (Exception ex)
            {
                FavCatMod.Logger.Error($"Exception in API sniffer patch: {ex}");
            }
        }
    }
}
