using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;
using Styletor.Utils;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC.UI.Core.Styles;
using File = Il2CppSystem.IO.File;

namespace Styletor.Styles
{
    public static class BuiltinStyleExporter
    {
        public static void ExportDefaultStyle(string baseDir, StyleEngine styleEngine)
        {
            var textAssetType = Il2CppType.Of<TextAsset>();
            var spriteType = Il2CppType.Of<Sprite>();
            
            MelonLogger.Msg($"Exporting default VRC skin to {baseDir}");
            foreach (var keyValuePair in styleEngine.field_Private_Dictionary_2_Tuple_2_String_Type_Object_0)
            {
                var basePath = Path.Combine(baseDir, keyValuePair.Key.Item1);
                
                if (keyValuePair.Key.Item2 == textAssetType)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
                    var textAsset = keyValuePair.Value.Cast<TextAsset>();
                    Il2CppSystem.IO.File.WriteAllBytes(basePath + ".txt", textAsset.bytes);
                } else if (keyValuePair.Key.Item2 == spriteType)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
                    var sprite = keyValuePair.Value.Cast<Sprite>();
                    SpriteSnipperUtil.SaveSpriteAsPngWithMetadata(sprite, basePath + ".png");
                }
            }
            MelonLogger.Msg($"Export finished");
        }

        public static void ExportObjectTree(GameObject root, string fileName)
        {
            var lines = new List<string>();
            var lineBuilder = new StringBuilder();

            void Dfs(Transform t, int depth)
            {
                lineBuilder.Append("".PadLeft(depth * 2));
                lineBuilder.Append(t.gameObject.name);

                var styles = t.GetComponents<StyleElement>();
                foreach (var styleElement in styles)
                {
                    var classes = styleElement.field_Public_String_1;
                    var tags = styleElement.field_Public_String_0;

                    if (!string.IsNullOrEmpty(classes))
                    {
                        lineBuilder.Append(" .");
                        lineBuilder.Append(classes);
                    }

                    if (!string.IsNullOrEmpty(tags))
                    {
                        lineBuilder.Append(" #");
                        lineBuilder.Append(tags);
                    }
                }
                
                var tChildCount = t.childCount;

                if (tChildCount <= 0)
                {
                    lines.Add(lineBuilder.ToString());
                    lineBuilder.Clear();
                    return;
                }
                
                lineBuilder.Append("{");
                
                lines.Add(lineBuilder.ToString());
                lineBuilder.Clear();

                for (var i = 0; i < tChildCount; i++) 
                    Dfs(t.GetChild(i), depth + 1);
                
                lines.Add("}".PadLeft(depth * 2 + 1));
            }
            
            Dfs(root.transform, 0);

            Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
            System.IO.File.WriteAllLines(fileName, lines, Encoding.UTF8);
        }
    }
}