using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using Styletor.API;
using Styletor.Jsons;
using UnityEngine;
using VRC.UI.Core.Styles;

#nullable enable

namespace Styletor.Styles
{
    public class StylesLoader
    {
        public const string StylesSubDir = "StyletorStyles";
        private readonly Dictionary<string, OverrideStyle> myStyles = new();
        private readonly Dictionary<string, OverrideStyle> myMixins = new();
        private readonly StyleEngineWrapper myStyleEngineWrapper;
        private readonly SettingsHolder mySettings;
        private readonly ColorizerManager myColorizer;

        public StylesLoader(StyleEngineWrapper styleEngineWrapper, SettingsHolder settings) 
        {
            myStyleEngineWrapper = styleEngineWrapper;
            mySettings = settings;

            myColorizer = new ColorizerManager(settings);
            
            ReloadStyles();

            MelonCoroutines.Start(WarmUpGrayImages());
        }

        internal IEnumerator WarmUpGrayImages()
        {
            yield return null;

            try
            {
                foreach (var overrideStyle in myStyles.Values)
                {
                    var it = overrideStyle.WarmUpGrayscaleImages();
                    while (it.MoveNext())
                        yield return it.Current;
                }
            }
            finally
            {
                FinishInitialization();
            }
        }

        internal void FinishInitialization()
        {
            mySettings.StyleEntry.OnValueChanged += (_, _) =>
            {
                ApplyStyle(mySettings.StyleEntry.Value);
            };

            mySettings.DisabledMixinsEntry.OnValueChanged += (_, _) =>
            {
                ApplyStyle(mySettings.StyleEntry.Value);
            };

            mySettings.BaseColorEntry.OnValueChanged += (_, _) => ApplyStyle(mySettings.StyleEntry.Value);
            mySettings.AccentColorEntry.OnValueChanged += (_, _) => ApplyStyle(mySettings.StyleEntry.Value);
            
            ApplyStyle(mySettings.StyleEntry.Value);
        }

        private void DoLoadStyle(string styleRawName, Func<OverrideStyle> loadDelegate)
        {
            try
            {
                var loaded = loadDelegate();
                (loaded.Metadata.IsMixin ? myMixins : myStyles)[styleRawName] = loaded;
            }
            catch (Exception ex)
            {
                StyletorMod.Instance.Logger.Warning($"Can't load style {styleRawName}: {ex}");
            }
        }

        internal void ReloadStyles()
        {
            foreach (var overrideStyle in myStyles.Values) overrideStyle.Dispose();
            myStyles.Clear();
            
            foreach (var overrideStyle in myMixins.Values) overrideStyle.Dispose();
            myMixins.Clear();

            var stylesDir = Path.Combine(MelonUtils.UserDataDirectory, StylesSubDir);
            if (!Directory.Exists(stylesDir)) return;
            stylesDir = Path.GetFullPath(stylesDir);
            foreach (var subdir in Directory.EnumerateDirectories(stylesDir, "*", SearchOption.TopDirectoryOnly))
                DoLoadStyle(subdir.Substring(stylesDir.Length + 1),
                    () => OverrideStyle.LoadFromFolder(myStyleEngineWrapper, subdir));

            foreach (var zipFile in Directory.EnumerateFiles(stylesDir, "*.zip", SearchOption.TopDirectoryOnly))
                DoLoadStyle(Path.GetFileNameWithoutExtension(zipFile),
                    () => OverrideStyle.LoadFromZip(myStyleEngineWrapper, zipFile));
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(it => !it.IsDynamic))
            foreach (var manifestResourceName in assembly.GetManifestResourceNames())
            {
                if (!manifestResourceName.EndsWith(".styletor.zip", StringComparison.InvariantCultureIgnoreCase)) continue;

                DoLoadStyle(manifestResourceName,
                    () => OverrideStyle.LoadFromZip(myStyleEngineWrapper, manifestResourceName,
                        assembly.GetManifestResourceStream(manifestResourceName)!));
            }
            
            foreach (var style in StyletorApi.StyleProviders.SelectMany(it => it()))
                DoLoadStyle(style.Key, () => OverrideStyle.LoadFromZip(myStyleEngineWrapper, style.Key, style.Value, true));

            var directOverrides = LoadDirectOverrides(stylesDir);
            if (directOverrides != null) myMixins["direct+overrides"] = directOverrides;

            RegenerateUixList();
        }

        internal IEnumerable<(string Id, string DisplayName)> GetKnownMixIns()
        {
            return myMixins.Select(it => (it.Key, it.Value.Metadata.Name));
        }

        internal void ApplyStyle(string styleName)
        {
            myStyleEngineWrapper.RestoreDefaultStyles();
            var disabledMixins = mySettings.DisabledMixinsEntry.Value.Split('|').ToHashSet();

            var mixinsToUse = myMixins.Where(it => !disabledMixins.Contains(it.Key)).Select(it => it.Value)
                .OrderBy(it => it.Metadata.MixinPriority).ToList();

            foreach (var overrideStyle in mixinsToUse.Where(it => it.Metadata.MixinPriority < 0))
                overrideStyle.ApplyOverrides(myColorizer);

            if (myStyles.TryGetValue(styleName, out var style))
            {
                StyletorMod.Instance.Logger.Msg($"Applying style {styleName}");
                style.ApplyOverrides(myColorizer);
            }
            else
                StyletorMod.Instance.Logger.Msg($"Style {styleName} not found");
            
            foreach (var overrideStyle in mixinsToUse.Where(it => it.Metadata.MixinPriority >= 0))
                overrideStyle.ApplyOverrides(myColorizer);
            
            myStyleEngineWrapper.UpdateStylesForSpriteOverrides();

            foreach (var styleElement in myStyleEngineWrapper.StyleEngine.GetComponentsInChildren<StyleElement>(true))
                styleElement.Method_Protected_Void_0(); // makes the element reload its styles
            
            foreach (var styleElement in GameObject.Find("UserInterface/MenuContent").GetComponentsInChildren<StyleElement>(true))
                styleElement.Method_Protected_Void_0(); // makes the element reload its styles
        }
        
        private OverrideStyle? LoadDirectOverrides(string stylesDir)
        {
            var images = Directory.EnumerateFiles(stylesDir, "*", SearchOption.TopDirectoryOnly)
                .Where(it => Path.GetExtension(it).ToLower() is ".jpg" or ".jpeg" or ".png").ToList();

            if (images.Count == 0) return null;
            
            var style = new OverrideStyle(myStyleEngineWrapper, new OverridesStyleSheet("<empty>", myStyleEngineWrapper), null, new StyleMetadata {Name = "Direct overrides", IsMixin = true, MixinPriority = 1_000_000});
            
            foreach (var imagePath in images)
            {
                var normalizedName = Path.GetFileNameWithoutExtension(imagePath);
                var spriteThisWouldReplace = myStyleEngineWrapper.TryFindOriginalSpriteByShortKey(normalizedName);
                var originalSpriteFullKey = myStyleEngineWrapper.GetSpriteFullNameByOriginalSprite(spriteThisWouldReplace);

                if (spriteThisWouldReplace == null || originalSpriteFullKey == null)
                {
                    StyletorMod.Instance.Logger.Msg($"Image {normalizedName} in StyletorStyles would replace nothing; it will be ignored");
                    continue;
                }

                var texture = Utils.Utils.LoadTexture(imagePath);
                if (texture == null)
                {
                    StyletorMod.Instance.Logger.Msg($"Could not load texture from image {normalizedName} in StyletorStyles");
                    continue;
                }

                texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                style.AttachResourceToDelete(texture);
                
                var rect = new Rect(0, 0, texture.width, texture.height);
                var pivot = Vector2.one / 2;
                var border = Vector4.zero;
                var sprite = Sprite.CreateSprite_Injected(texture, ref rect, ref pivot, 200f, 0,
                    SpriteMeshType.FullRect, ref border, false);

                sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                style.AttachResourceToDelete(sprite);
                style.AddSpriteOverride(originalSpriteFullKey, sprite);
            }
            
            return style;
        }

        private void RegenerateUixList()
        {
            mySettings.EnumSettingsInfo.Clear();
            mySettings.EnumSettingsInfo.Add(("default", "VRChat Default"));
            
            foreach (var keyValuePair in myStyles)
                mySettings.EnumSettingsInfo.Add((keyValuePair.Key, keyValuePair.Value.Metadata.Name));
        }
    }
}