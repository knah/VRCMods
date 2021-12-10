using System.Collections;
using System.Linq;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Styletor.Utils;
using UIExpansionKit;
using UnityEngine;
using UnityEngine.UI;

namespace Styletor.ExtraHandlers
{
    public class ActionMenuHandler
    {
        private readonly SettingsHolder mySettings;
        
        private readonly GameObject myLeftMenuRoot;
        private readonly GameObject myRightMenuRoot;
        private readonly GameObject myPedalPrefab;
        private readonly GameObject myDividerPrefab;

        private readonly Dictionary<Texture2D, Texture2D> myGrayTexturesToColorTextures = new();
        private readonly Dictionary<Texture2D, Texture2D> myColorTexturesToGrayTextures = new();
        
        private readonly Dictionary<Sprite, Sprite> myGraySpritesToColorSprites = new();
        private readonly Dictionary<Sprite, Sprite> myColorSpritesToGraySprites = new();

        private readonly System.Collections.Generic.Dictionary<string, Color> myOriginalGraphicColorsByObjectName = new();

        private readonly System.Collections.Generic.Dictionary<string, Texture2D> myTexturesByName = new();
        private readonly System.Collections.Generic.Dictionary<string, Sprite> mySpritesByName = new();

        private static readonly System.Collections.Generic.HashSet<string> ourTexturesToGrayscale = new() { "background_main", "background_puppet", "divider", "joystick", "arrow" };

        private static readonly System.Collections.Generic.HashSet<string> ourDarkObjectNames = new() { "Main/Center", "Container/Center" };
        private static readonly System.Collections.Generic.HashSet<string> ourAccentObjectNames = new()
        {
            "PedalOption/Select", "/Select", "PedalOption(Clone)/Select", "Inner/Folder Icon", "Inner/Status Icon", "Inner/Playing", "Inner/Waiting",
            "Main/Cursor", "Container/Fill", "Container/Cursor", "Container/Arrow",
            "Container/Fill Up", "Container/Fill Down", "Container/Fill Right", "Container/Fill Left"
        };
        private static readonly System.Collections.Generic.HashSet<string> ourBaseObjectNames = new() { "Main/Background", "Divider/Image", "/Image", "Divider/Divider", "Divider(Clone)/Image", "Inner/Center", "Container/Background", "Container/Title"  };

        public ActionMenuHandler(SettingsHolder settings)
        {
            mySettings = settings;
            myLeftMenuRoot = UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/ActionMenu/Container/MenuL/ActionMenu")!; 
            myRightMenuRoot = UnityUtils.FindInactiveObjectInActiveRoot("UserInterface/ActionMenu/Container/MenuR/ActionMenu")!;

            var leftAmComponent = myLeftMenuRoot.GetComponent<ActionMenu>();
            myPedalPrefab = typeof(ActionMenu)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(it => it.PropertyType == typeof(GameObject))
                .Select(it => (GameObject)it.GetValue(leftAmComponent)).Single(it => it?.name == "PedalOption");
            
            myDividerPrefab = typeof(ActionMenu)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(it => it.PropertyType == typeof(GameObject))
                .Select(it => (GameObject)it.GetValue(leftAmComponent)).Single(it => it?.name == "Divider");
            
            MelonCoroutines.Start(PrepareActionMenuBackup());
        }

        private IEnumerator PrepareActionMenuBackup()
        {
            ProcessObjectForSave(myLeftMenuRoot.transform, "");
            ProcessObjectForSave(myRightMenuRoot.transform, "");
            ProcessObjectForSave(myPedalPrefab.transform, "");
            ProcessObjectForSave(myDividerPrefab.transform, "");

            yield return null;
            
            foreach (var textureName in ourTexturesToGrayscale)
            {
                if (myTexturesByName.TryGetValue(textureName, out var texture))
                {
                    var grayscaled = SpriteSnipperUtil.GetGrayscaledTexture(texture, true);

                    myGrayTexturesToColorTextures[grayscaled] = texture;
                    myColorTexturesToGrayTextures[texture] = grayscaled;
                    
                    yield return null;
                }
                
                if (mySpritesByName.TryGetValue(textureName, out var sprite))
                {
                    var grayscaled = SpriteSnipperUtil.GetGrayscaledSprite(sprite, true);

                    myGraySpritesToColorSprites[grayscaled] = sprite;
                    myColorSpritesToGraySprites[sprite] = grayscaled;

                    yield return null;
                }
            }

            InitializeSettingsBinding();
        }

        private void InitializeSettingsBinding()
        {
            mySettings.RegisterUpdateDelegate(mySettings.ActionMenuModeEntry, UpdateSkinning, mySettings.ActionMenuBaseColorEntry, mySettings.ActionMenuAccentColorEntry);

            UpdateSkinning();
        }

        private void RevertToDefault()
        {
            ProcessObjectForRevert(myLeftMenuRoot.transform, "");
            ProcessObjectForRevert(myRightMenuRoot.transform, "");
            ProcessObjectForRevert(myPedalPrefab.transform, "");
            ProcessObjectForRevert(myDividerPrefab.transform, "");
        }

        private void ApplyRecolors()
        {
            var baseColor = mySettings.GetColorForMode(mySettings.ActionMenuModeEntry, mySettings.BaseColorEntry, mySettings.ActionMenuBaseColorEntry)!.Value;
            var iconColor = mySettings.GetColorForMode(mySettings.ActionMenuModeEntry, mySettings.AccentColorEntry, mySettings.ActionMenuAccentColorEntry)!.Value;

            ProcessObjectForApply(myLeftMenuRoot.transform, "", ref baseColor, ref iconColor);
            ProcessObjectForApply(myRightMenuRoot.transform, "", ref baseColor, ref iconColor);
            ProcessObjectForApply(myPedalPrefab.transform, "", ref baseColor, ref iconColor);
            ProcessObjectForApply(myDividerPrefab.transform, "", ref baseColor, ref iconColor);
        }

        private void UpdateSkinning()
        {
            if (mySettings.ActionMenuModeEntry.Value == SettingsHolder.MultiColorMode.DoNotRecolor)
                RevertToDefault();
            else
                ApplyRecolors();
        }

        private void EncacheTexture(Texture2D? texture)
        {
            if (texture == null) return;

            var name = texture.name;
            
            if (myTexturesByName.TryGetValue(name, out var previous) && previous != texture)
                MelonLogger.Msg($"Object named {name} as a texture different from previous one: {previous.name} != {texture.name}");

            myTexturesByName[name] = texture;
        }
        
        private Texture2D? RestoreTexture(Texture2D? texture)
        {
            if (texture == null) return null;
            return myGrayTexturesToColorTextures.ContainsKey(texture) ? myGrayTexturesToColorTextures[texture] : texture;
        }

        private Sprite? RestoreSprite(Sprite? sprite)
        {
            if (sprite == null) return null;
            return myGraySpritesToColorSprites.ContainsKey(sprite) ? myGraySpritesToColorSprites[sprite] : sprite;
        }

        private Texture2D? ApplyTexture(Texture2D? texture)
        {
            if (texture == null) return null;
            return myColorTexturesToGrayTextures.ContainsKey(texture) ? myColorTexturesToGrayTextures[texture] : texture;
        }

        private Sprite? ApplySprite(Sprite? sprite)
        {
            if (sprite == null) return null;
            return myColorSpritesToGraySprites.ContainsKey(sprite) ? myColorSpritesToGraySprites[sprite] : sprite;
        }

        private void EncacheSprite(Sprite? texture)
        {
            if (texture == null) return;

            var name = texture.name;
            
            if (mySpritesByName.TryGetValue(name, out var previous) && previous != texture)
                MelonLogger.Msg($"Object named {name} as a texture different from previous one: {previous.name} != {texture.name}");

            mySpritesByName[name] = texture;
        }

        private void ProcessObjectForSave(Transform t, string parentName)
        {
            var name = t.gameObject.name;
            var fullName = parentName + "/" + name;
            
            var graphics = t.GetComponents<Graphic>();
            if (graphics.Count > 0)
            {
                if (graphics.Count > 1)
                    MelonLogger.Msg($"AM object {fullName} (in {t.parent.gameObject.name}) has more than one Graphic!");

                var color = graphics[0].color;
                if (myOriginalGraphicColorsByObjectName.TryGetValue(fullName, out var oldColor) && oldColor != color)
                    MelonLogger.Msg($"Object named {fullName} was seen with two different colors: {oldColor.ToString()} vs {color.ToString()}");

                myOriginalGraphicColorsByObjectName[fullName] = color;
                
                foreach (var graphic in graphics)
                {
                    var maybePedalImage = graphic.TryCast<PedalGraphic>();
                    if (maybePedalImage != null) 
                        EncacheTexture(maybePedalImage._texture?.Cast<Texture2D>());

                    var maybeRawImage = graphic.TryCast<RawImage>();
                    if (maybeRawImage != null) 
                        EncacheTexture(maybeRawImage.texture?.Cast<Texture2D>());

                    var maybeNormalImage = graphic.TryCast<Image>();
                    if (maybeNormalImage != null)
                        EncacheSprite(maybeNormalImage.sprite);
                }
            }

            var childCount = t.childCount;
            for (var i = 0; i < childCount; i++) ProcessObjectForSave(t.GetChild(i), name);
        }
        
        
        private void ProcessObjectForRevert(Transform t, string parentName)
        {
            var name = t.gameObject.name;
            var fullName = parentName + "/" + name;
            
            var graphics = t.GetComponents<Graphic>();
            if (graphics.Count > 0)
            {
                if (myOriginalGraphicColorsByObjectName.TryGetValue(fullName, out var color))
                    foreach (var graphic in graphics)
                        graphic.color = color;

                foreach (var graphic in graphics)
                {
                    var maybePedalImage = graphic.TryCast<PedalGraphic>();
                    if (maybePedalImage != null)
                        maybePedalImage._texture = RestoreTexture(maybePedalImage._texture?.Cast<Texture2D>());

                    var maybeRawImage = graphic.TryCast<RawImage>();
                    if (maybeRawImage != null)
                        maybeRawImage.texture = RestoreTexture(maybeRawImage.texture?.Cast<Texture2D>());
                    

                    var maybeNormalImage = graphic.TryCast<Image>();
                    if (maybeNormalImage != null)
                        maybeNormalImage.sprite = RestoreSprite(maybeNormalImage.sprite);
                }
            }

            var childCount = t.childCount;
            for (var i = 0; i < childCount; i++) ProcessObjectForRevert(t.GetChild(i), name);
        }
        
        private void ProcessObjectForApply(Transform t, string parentName, ref Color baseColor, ref Color iconColor)
        {
            var name = t.gameObject.name;
            var fullName = parentName + "/" + name;
            
            var graphics = t.GetComponents<Graphic>();
            if (graphics.Count > 0)
            {
                foreach (var graphic in graphics)
                {
                    var maybePedalImage = graphic.TryCast<PedalGraphic>();
                    if (maybePedalImage != null)
                        maybePedalImage._texture = ApplyTexture(maybePedalImage._texture?.Cast<Texture2D>());

                    var maybeRawImage = graphic.TryCast<RawImage>();
                    if (maybeRawImage != null)
                        maybeRawImage.texture = ApplyTexture(maybeRawImage.texture?.Cast<Texture2D>());
                    

                    var maybeNormalImage = graphic.TryCast<Image>();
                    if (maybeNormalImage != null)
                        maybeNormalImage.sprite = ApplySprite(maybeNormalImage.sprite);
                    
                    if (ourBaseObjectNames.Contains(fullName))
                        graphic.RecolorKeepAlpha(baseColor);
                    else if (ourAccentObjectNames.Contains(fullName))
                        graphic.RecolorKeepAlpha(iconColor);
                    else if (ourDarkObjectNames.Contains(fullName))
                        graphic.RecolorKeepAlpha(baseColor.RGBMultipliedClamped(0.5f));
                }
            }

            var childCount = t.childCount;
            for (var i = 0; i < childCount; i++) ProcessObjectForApply(t.GetChild(i), name, ref baseColor, ref iconColor);
        }
    }
}