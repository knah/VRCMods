using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using Styletor.Utils;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.UI.Core.Styles;
using Object = UnityEngine.Object;

#nullable enable

namespace Styletor
{
    public class StyleEngineWrapper
    {
        public readonly StyleEngine StyleEngine;
        
        private readonly List<(int styleIndex, ulong selectorPriority, Selector selector, List<(int, StyleElement.ValueTypeNPublicSealedInSiBoInCoObObUnique)> Properties)> myOriginalStylesBackup = new();
        private readonly Dictionary<string, Sprite> myOriginalSprites = new();
        private readonly Dictionary<string, Sprite> myOriginalSpritesByLowercaseShortKey = new();
        private readonly Dictionary<string, Sprite> myOriginalSpritesByLowercaseFullKey = new();
        private readonly Dictionary<Il2CppSystem.Tuple<string, Il2CppSystem.Type>, Object> myOriginalResourcesInAllResourcesMap = new();

        private readonly Dictionary<string, List<ElementStyle>> myStylesCache = new();
        private readonly Dictionary<string, string> myNormalizedToActualSpriteNames = new();

        private readonly Il2CppSystem.Collections.Generic.Dictionary<Sprite, Sprite> mySpriteOverrideDict = new();

        public StyleEngineWrapper(StyleEngine styleEngine)
        {
            StyleEngine = styleEngine;
        }

        public List<ElementStyle>? TryGetBySelector(string normalizedSelector)
        {
            return myStylesCache.TryGetValue(normalizedSelector, out var result) ? result : null;
        }

        public Sprite? TryFindOriginalSprite(string key)
        {
            return myOriginalSpritesByLowercaseFullKey.TryGetValue(key, out var result) ? result : null;
        }
        
        public Sprite? TryFindOriginalSpriteByShortKey(string key)
        {
            return myOriginalSpritesByLowercaseShortKey.TryGetValue(key, out var result) ? result : null;
        }

        public void OverrideSprite(string key, Sprite sprite)
        {
            var keyLastPart = Path.GetFileName(key); 
            var actualKey = myNormalizedToActualSpriteNames.TryGetValue(keyLastPart, out var normalized) ? normalized : keyLastPart;

            var originalSprite = TryFindOriginalSprite(key);
            if (originalSprite != null) mySpriteOverrideDict[originalSprite] = sprite;

            StyleEngine.field_Private_Dictionary_2_String_Sprite_0[actualKey] = sprite;
            StyleEngine.field_Private_Dictionary_2_Tuple_2_String_Type_Object_0[
                new Il2CppSystem.Tuple<string, Il2CppSystem.Type>(key.ToLower(), Il2CppType.Of<Sprite>())] = sprite;
        }

        internal void UpdateStylesForSpriteOverrides()
        {
            var writeAccumulator = new List<(int, StyleElement.ValueTypeNPublicSealedInSiBoInCoObObUnique)>();
            
            foreach (var elementStyle in StyleEngine.field_Private_List_1_ElementStyle_0)
            {
                foreach (var keyValuePair in elementStyle.field_Public_Dictionary_2_Int32_ValueTypeNPublicSealedInSiBoInCoObObUnique_0)
                {
                    var styleProperty = keyValuePair.Value;
                    var maybeSprite = styleProperty.field_Public_Object_0?.TryCast<Sprite>();
                    if (maybeSprite == null || !mySpriteOverrideDict.ContainsKey(maybeSprite)) continue;

                    styleProperty.field_Public_Object_0 = mySpriteOverrideDict[maybeSprite];
                    writeAccumulator.Add((keyValuePair.Key, styleProperty));
                }

                // ah yes, ConcurrentModificationException
                foreach (var (k, v) in writeAccumulator)
                    elementStyle.field_Public_Dictionary_2_Int32_ValueTypeNPublicSealedInSiBoInCoObObUnique_0[k] = v;
                
                writeAccumulator.Clear();
            }
        }
        
        internal void RestoreDefaultStyles()
        {
            mySpriteOverrideDict.Clear();
            
            var styles = StyleEngine.field_Private_List_1_ElementStyle_0;
            for (var i = 0; i < myOriginalStylesBackup.Count; i++)
            {
                var style = styles[i];
                var backup = myOriginalStylesBackup[i];
                style.field_Public_Int32_0 = backup.styleIndex;
                style.field_Public_UInt64_0 = backup.selectorPriority;
                style.field_Public_Selector_0 = backup.selector;
                var propsDict = style.field_Public_Dictionary_2_Int32_ValueTypeNPublicSealedInSiBoInCoObObUnique_0;
                propsDict.Clear();
                foreach (var (key, value) in backup.Properties) 
                    propsDict[key] = value;
            }

            var spriteDict = StyleEngine.field_Private_Dictionary_2_String_Sprite_0;
            foreach (var keyValuePair in myOriginalSprites) 
                spriteDict[keyValuePair.Key] = keyValuePair.Value;

            var resMap = StyleEngine.field_Private_Dictionary_2_Tuple_2_String_Type_Object_0;
            foreach (var keyValuePair in myOriginalResourcesInAllResourcesMap)
                resMap[keyValuePair.Key] = keyValuePair.Value;
        }

        internal void BackupDefaultStyle()
        {
            foreach (var elementStyle in StyleEngine.field_Private_List_1_ElementStyle_0)
            {
                var innerList = new List<(int, StyleElement.ValueTypeNPublicSealedInSiBoInCoObObUnique)>();
                foreach (var keyValuePair in elementStyle.field_Public_Dictionary_2_Int32_ValueTypeNPublicSealedInSiBoInCoObObUnique_0)
                    innerList.Add((keyValuePair.Key, keyValuePair.Value));
                myOriginalStylesBackup.Add((elementStyle.field_Public_Int32_0, elementStyle.field_Public_UInt64_0, elementStyle.field_Public_Selector_0, innerList));

                var normalizedSelector = elementStyle.field_Public_Selector_0.ToStringNormalized();
                
                if (myStylesCache.TryGetValue(normalizedSelector, out var existing)) 
                    existing.Add(elementStyle);
                else
                    myStylesCache[normalizedSelector] = new List<ElementStyle>() { elementStyle };
            }

            foreach (var keyValuePair in StyleEngine.field_Private_Dictionary_2_String_Sprite_0)
            {
                var key = keyValuePair.Key;
                var normalizedKey = key.ToLower();
                var sprite = keyValuePair.Value;
                
                myOriginalSprites[key] = sprite;
                myOriginalSpritesByLowercaseShortKey[normalizedKey] = sprite;
                myNormalizedToActualSpriteNames[normalizedKey] = key;
            }

            foreach (var keyValuePair in StyleEngine.field_Private_Dictionary_2_Tuple_2_String_Type_Object_0)
            {
                myOriginalResourcesInAllResourcesMap[keyValuePair.Key] = keyValuePair.Value;
                if (keyValuePair.Key.Item2 == Il2CppType.Of<Sprite>())
                    myOriginalSpritesByLowercaseFullKey[keyValuePair.Key.Item1] = keyValuePair.Value.Cast<Sprite>();
            }

            MelonLogger.Msg($"Stored default style: {myOriginalStylesBackup.Count} styles, {myOriginalSprites.Count} sprites");
        }

        public string? GetSpriteFullNameByOriginalSprite(Sprite? originalSprite)
        {
            if (originalSprite == null) return null;
            return myOriginalSpritesByLowercaseFullKey.FirstOrDefault(it => it.Value == originalSprite).Key;
        }
    }
}