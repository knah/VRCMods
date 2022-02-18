using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using MelonLoader;
using MelonLoader.TinyJSON;
using Styletor.Jsons;
using Styletor.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Styletor.Styles
{
    public class OverrideStyle : IDisposable
    {
        private readonly OverridesStyleSheet myStyleSheet;
        private readonly OverridesStyleSheet? mySecondarySheet;
        private readonly StyleEngineWrapper myStyleEngineWrapper;
        private readonly Dictionary<string, Sprite> myOverrideSprites = new();
        
        private readonly List<Object> myObjectsToDelete = new ();

        public readonly StyleMetadata Metadata;

        internal OverrideStyle(StyleEngineWrapper styleEngineWrapper, OverridesStyleSheet styleSheet, OverridesStyleSheet? secondarySheet, StyleMetadata metadata)
        {
            myStyleSheet = styleSheet;
            mySecondarySheet = secondarySheet;
            Metadata = metadata;
            myStyleEngineWrapper = styleEngineWrapper;
        }

        internal IEnumerator WarmUpGrayscaleImages()
        {
            foreach (var spriteName in Metadata.SpritesToGrayscale)
            {
                var originalSprite = myStyleEngineWrapper.TryFindOriginalSprite(spriteName);
                if (originalSprite == null) continue;

                SpriteSnipperUtil.GetGrayscaledSprite(originalSprite, true);

                yield return null;
            }
        }

        public void ApplyOverrides(ColorizerManager colorizer)
        {
            foreach (var spriteName in Metadata.SpritesToGrayscale)
            {
                var originalSprite = myStyleEngineWrapper.TryFindOriginalSprite(spriteName);
                if (originalSprite == null) continue;

                var grayscaled = SpriteSnipperUtil.GetGrayscaledSprite(originalSprite, true);
                myStyleEngineWrapper.OverrideSprite(spriteName, grayscaled);
            }
            
            foreach (var keyValuePair in myOverrideSprites)
                myStyleEngineWrapper.OverrideSprite(keyValuePair.Key, keyValuePair.Value);

            myStyleSheet.ApplyOverrides(colorizer);
            mySecondarySheet?.ApplyOverrides(colorizer);
        }

        public static OverrideStyle LoadFromStreams(StyleEngineWrapper styleEngine, Dictionary<string, Stream> streamMap, string fallbackName, bool closeStreams = false)
        {
            StyleMetadata metadata;
            if (streamMap.TryGetValue("info.json", out var infoStream))
                JSON.MakeInto(JSON.Load(infoStream.ReadAllText()), out metadata);
            else
                metadata = new StyleMetadata();

            if (metadata.Name == StyleMetadata.UnnamedName) metadata.Name = Path.GetFileName(fallbackName);
            
            
            var styleSheet = streamMap.TryGetValue("overrides.vrcss", out var styleStream)
                ? OverridesStyleSheet.ParseFrom(styleEngine, metadata.Name, styleStream.ReadAllLines()) 
                : new OverridesStyleSheet(metadata.Name, styleEngine);

            var secondaryStyleSheet = streamMap.TryGetValue("overrides-secondpass.vrcss", out var secondaryStyleStream)
                ? OverridesStyleSheet.ParseFrom(styleEngine, metadata.Name, secondaryStyleStream.ReadAllLines()) 
                : null;

            var result = new OverrideStyle(styleEngine, styleSheet, secondaryStyleSheet, metadata);
            
            foreach (var keyValuePair in streamMap)
            {
                if (!keyValuePair.Key.EndsWith(".png")) continue;
                
                var loadedTexture = Utils.Utils.LoadTexture(keyValuePair.Value);
                if (loadedTexture == null)
                {
                    MelonLogger.Msg($"Failed to load a texture from {keyValuePair.Key}");
                    continue;
                }

                loadedTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                result.myObjectsToDelete.Add(loadedTexture);
                
                var spriteConfigPath = keyValuePair.Key + ".json";
                SpriteJson? spriteJson = null;
                if (streamMap.TryGetValue(spriteConfigPath, out var spriteConfigStream))
                    JSON.MakeInto(JSON.Load(spriteConfigStream.ReadAllText()), out spriteJson);
                spriteJson ??= new SpriteJson();

                var rect = new Rect(0, 0, loadedTexture.width, loadedTexture.height);
                var pivot = new Vector2(spriteJson.PivotX, spriteJson.PivotY);
                var ppu = spriteJson.PixelsPerUnit;

                var border = new Vector4(spriteJson.BorderLeft, spriteJson.BorderBottom, spriteJson.BorderRight, spriteJson.BorderTop);
                
                var sprite = Sprite.CreateSprite_Injected(loadedTexture, ref rect, ref pivot, ppu, 0, SpriteMeshType.FullRect, ref border, false);
                sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                result.myObjectsToDelete.Add(sprite);

                var key = keyValuePair.Key;
                key = key.Substring(0, key.Length - 4).Replace('\\', '/');
                
                MelonDebug.Msg($"Loaded debug sprite {key}");
                result.myOverrideSprites[key] = sprite;
            }
            
            if (closeStreams)
            {
                foreach (var streamMapValue in streamMap.Values) 
                    streamMapValue.Dispose();
            }

            return result;
        }
        
        internal void AddSpriteOverride(string key, Sprite value) => myOverrideSprites[key] = value;

        public static OverrideStyle LoadFromZip(StyleEngineWrapper styleEngine, string zipFile)
        {
            return LoadFromZip(styleEngine, Path.GetFileNameWithoutExtension(zipFile), File.OpenRead(zipFile), true);
        }

        public static OverrideStyle LoadFromZip(StyleEngineWrapper styleEngine, string fallbackName, Stream stream, bool closeStream = false)
        {
            using var zipStream = new ZipArchive(stream, ZipArchiveMode.Read, !closeStream, Encoding.UTF8);
            var entriesDict = zipStream.Entries.ToDictionary(it => it.FullName.ToLower().Replace('\\', '/'), it => it.Open());

            return LoadFromStreams(styleEngine, entriesDict, fallbackName, true);
        }

        public static OverrideStyle LoadFromFolder(StyleEngineWrapper styleEngine, string folderPath)
        {
            folderPath = Path.GetFullPath(folderPath);

            var allFiles = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories);
            var streamMap = allFiles.ToDictionary(it => it.Substring(folderPath.Length + 1).ToLower().Replace('\\', '/'), s => (Stream) File.OpenRead(s));

            return LoadFromStreams(styleEngine, streamMap, Path.GetFileName(folderPath), true);
        }

        public void Dispose()
        {
            foreach (var obj in myObjectsToDelete) 
                Object.Destroy(obj);
            myObjectsToDelete.Clear();
            myOverrideSprites.Clear();
        }

        public void AttachResourceToDelete(Object obj) => myObjectsToDelete.Add(obj);
    }
}