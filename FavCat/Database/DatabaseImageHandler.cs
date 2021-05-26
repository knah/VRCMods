using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FavCat.Database.Stored;
using LiteDB;
using MelonLoader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UIExpansionKit.API;
using UnhollowerBaseLib;
using UnityEngine;

namespace FavCat.Database
{
    public class DatabaseImageHandler
    {
        private readonly LiteDatabase myFileDatabase;
        private readonly ILiteCollection<StoredImageInfo> myImageInfos;

        public DatabaseImageHandler(LiteDatabase fileDatabase)
        {
            myFileDatabase = fileDatabase;
            myImageInfos = fileDatabase.GetCollection<StoredImageInfo>();
        }

        public bool Exists(string url)
        {
            return myFileDatabase.FileStorage.Exists(url);
        }

        public Task TrimCache(long maxSize)
        {
            return Task.Run(async () =>
            {
                MelonDebug.Msg("Trimming image cache");
                var allFileInfos = new List<(LiteFileInfo<string>, StoredImageInfo)>();
                var runningSums = new List<long>();
                foreach (var liteFileInfo in myFileDatabase.FileStorage.FindAll())
                {
                    if (string.IsNullOrEmpty(liteFileInfo.Id)) continue;
                    
                    allFileInfos.Add((liteFileInfo,
                        myImageInfos.FindById(liteFileInfo.Id) ?? new StoredImageInfo
                            {LastAccessed = DateTime.MinValue, Id = liteFileInfo.Id}));
                }

                allFileInfos.Sort((a, b) => a.Item2.LastAccessed.CompareTo(b.Item2.LastAccessed));
                long totalSize = 0;
                foreach (var fileInfo in allFileInfos)
                {
                    totalSize += fileInfo.Item1.Length;
                    runningSums.Add(totalSize);
                }

                if (totalSize < maxSize)
                {
                    MelonDebug.Msg("Cache already under limit");
                    return;
                }

                var cutoffPoint = runningSums.BinarySearch(maxSize);
                if (cutoffPoint < 0)
                    cutoffPoint = ~cutoffPoint;

                for (var i = 0; i < cutoffPoint; i++)
                {
                    myFileDatabase.FileStorage.Delete(allFileInfos[i].Item1.Id);
                    myImageInfos.Delete(allFileInfos[i].Item2.Id);
                }

                await TaskUtilities.YieldToMainThread();
                
                MelonLogger.Msg($"Removed {cutoffPoint} images from cache");
            });
        }

        public async Task LoadImageAsync(string url, Action<Texture2D?> onDone)
        {
            try
            {
                if (!myFileDatabase.FileStorage.Exists(url))
                {
                    onDone(null);
                    return;
                }

                await Task.Run(() => { }).ConfigureAwait(false);

                using var imageStream = myFileDatabase.FileStorage.OpenRead(url);
                using var image = await Image.LoadAsync<Rgba32>(imageStream).ConfigureAwait(false);

                var newImageInfo = new StoredImageInfo {Id = url, LastAccessed = DateTime.UtcNow};
                myImageInfos.Upsert(newImageInfo);

                await TaskUtilities.YieldToMainThread();

                try
                {
                    onDone(CreateTextureFromImage(image));
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Exception in onDone callback: {ex}");
                }
            }
            catch (Exception ex)
            {
                if (MelonDebug.IsEnabled())
                    MelonLogger.Warning($"Exception in image load, will delete offending image: {ex}");
                myFileDatabase.FileStorage.Delete(url);
                myImageInfos.Delete(url);
                onDone(AssetsHandler.PreviewLoading.texture);
            }
        }

        private static unsafe Texture2D CreateTextureFromImage(Image<Rgba32> image)
        {
            image.Mutate(processor => processor.Flip(FlipMode.Vertical));
            image.TryGetSinglePixelSpan(out var pixels);

            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false, false);
            fixed (void* pixelsPtr = pixels)
                texture.LoadRawTextureData((IntPtr) pixelsPtr, pixels.Length * 4);

            texture.Apply(false, true);

            return texture;
        }

        public Task StoreImageAsync(string url, Il2CppStructArray<byte> data)
        {
            if (string.IsNullOrEmpty(url)) return Task.CompletedTask;
            
            return Task.Run(() =>
            {
                try
                {
                    if (myFileDatabase.FileStorage.Exists(url))
                        return;

                    myFileDatabase.FileStorage.Upload(url, url, new MemoryStream(data));

                    var newImageInfo = new StoredImageInfo {Id = url, LastAccessed = DateTime.MinValue};
                    myImageInfos.Upsert(newImageInfo);
                }
                catch (LiteException ex)
                {
                    if (MelonDebug.IsEnabled())
                        MelonLogger.Warning($"Database exception in image store handler: {ex}");
                }
            });
        }
    }
}