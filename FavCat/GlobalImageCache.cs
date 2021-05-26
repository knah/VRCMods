using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;

namespace FavCat
{
    public static class GlobalImageCache
    {
        private const int MaxRunningRequests = 2;
        private const float RequestDelay = .1f;
        
        private static readonly Dictionary<string, List<Action<Texture2D>>> QueuedRequests = new Dictionary<string, List<Action<Texture2D>>>();
        private static readonly Queue<string> RequestOrderQueue = new Queue<string>();
        private static readonly Dictionary<string, List<Action<Texture2D>>> InFlightRequests = new Dictionary<string, List<Action<Texture2D>>>();
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        
        public static void DownloadImage(string? url, Action<Texture2D> onDone)
        {
            if (url == null)
            {
                onDone(AssetsHandler.PreviewError.texture);
                return;
            }

            if (Textures.TryGetValue(url, out var tex) && tex)
            {
                onDone(tex);
                return;
            }

            if (InFlightRequests.TryGetValue(url, out var inFlightList))
            {
                inFlightList.Add(onDone);
                return;
            }

            if (QueuedRequests.TryGetValue(url, out var queueList))
            {
                queueList.Add(onDone);
                return;
            }

            QueuedRequests[url] = new List<Action<Texture2D>> {onDone};
            RequestOrderQueue.Enqueue(url);
        }

        private static void RunRequest(string url)
        {
            if (!QueuedRequests.TryGetValue(url, out var list))
                return;

            QueuedRequests.Remove(url);

            if (list.Count == 0)
                return;

            InFlightRequests[url] = list;

            var trueUri = url;
            if (FavCatMod.Database.ImageHandler.Exists(trueUri))
            {
                FavCatMod.Database.ImageHandler.LoadImageAsync(trueUri, tex =>
                {
                    if (!Textures.TryGetValue(url, out var oldTex) || !oldTex)
                    {
                        Textures[url] = tex;
                    }

                    if (!InFlightRequests.TryGetValue(url, out var list))
                        return;

                    InFlightRequests.Remove(url);
                
                    foreach (var action in list) 
                        action(tex);
                }).NoAwait();
                return;
            }
            
            ourNextAllowedUpdate = Time.time + RequestDelay;
            
            MelonLogger.Msg($"Performing image request to {url}");
            ImageDownloader.DownloadImage(url, 256, new Action<Texture2D>(tex =>
            {
                if (!Textures.TryGetValue(url, out var oldTex) || !oldTex)
                {
                    Textures[url] = tex;
                    if (FavCatSettings.CacheImagesInMemory)
                        tex.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }

                if (!InFlightRequests.TryGetValue(url, out var list))
                    return;

                InFlightRequests.Remove(url);
                
                foreach (var action in list) 
                    action(tex);
            }), new Action(() =>
            {
                if (!Textures.TryGetValue(url, out var oldTex) || !oldTex)
                    Textures[url] = AssetsHandler.PreviewError.texture;

                if (!InFlightRequests.TryGetValue(url, out var list))
                    return;

                InFlightRequests.Remove(url);
                
                foreach (var action in list) 
                    action(AssetsHandler.PreviewError.texture);
            }));
        }

        private static IEnumerator MockDownloadImage(string url, Action<Texture2D> onDone)
        {
            yield return new WaitForSeconds(0.5f);

            onDone(AssetsHandler.IconUni.texture);
        }

        private static float ourNextAllowedUpdate;

        // This might have been better implemented as a coroutine, but MelonCoroutines are implemented via Update anyway
        public static void OnUpdate()
        {
            if (RequestOrderQueue.Count == 0)
                return;
            
            if (InFlightRequests.Count >= MaxRunningRequests)
                return;

            var currentTime = Time.time;
            if (currentTime < ourNextAllowedUpdate)
                return;
            
            RunRequest(RequestOrderQueue.Dequeue());
        }

        public static void CancelRequest(string? url, Action<Texture2D> callback)
        {
            if (url == null) return;
            
            if (QueuedRequests.TryGetValue(url, out var queuedList))
                queuedList.Remove(callback);

            if (InFlightRequests.TryGetValue(url, out var inFlightList))
                inFlightList.Remove(callback);
        }

        public static void Drop(string url)
        {
            if (Textures.TryGetValue(url, out var tex) && tex)
            {
                tex.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
                Textures.Remove(url);
            }
        }
    }
}