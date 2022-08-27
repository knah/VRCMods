using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;
using Styletor.Utils;
using UnhollowerBaseLib;
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
            var audioClipType = Il2CppType.Of<AudioClip>();
            
            StyletorMod.Instance.Logger.Msg($"Exporting default VRC skin to {baseDir}");
            foreach (var keyValuePair in styleEngine.field_Private_Dictionary_2_Tuple_2_String_Type_Object_0)
            {
                var basePath = Path.Combine(baseDir, keyValuePair.Key.Item1);
                
                if (keyValuePair.Key.Item2 == textAssetType)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
                    var textAsset = keyValuePair.Value.Cast<TextAsset>();
                    File.WriteAllBytes(basePath + ".txt", textAsset.bytes);
                } else if (keyValuePair.Key.Item2 == spriteType)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
                    var sprite = keyValuePair.Value.Cast<Sprite>();
                    SpriteSnipperUtil.SaveSpriteAsPngWithMetadata(sprite, basePath + ".png");
                }
                else if (keyValuePair.Key.Item2 == audioClipType)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(basePath)!);
                    var audioClip = keyValuePair.Value.Cast<AudioClip>();
                    WriteWaveFile(basePath + ".wav", audioClip);
                }
            }
            StyletorMod.Instance.Logger.Msg($"Export finished");
        }

        private static void WriteWaveFile(string filePath, AudioClip clip)
        {
            using var binaryWriter = new BinaryWriter(System.IO.File.OpenWrite(filePath), Encoding.UTF8, false);

            var totalData = clip.samples * 4 * clip.channels;
            
            binaryWriter.Write(0x46464952); // 0x 52 49 46 46 BE
            binaryWriter.Write(38 + totalData);
            binaryWriter.Write(0x45564157); // 0x 57 41 56 45 BE
            
            binaryWriter.Write(0x20746d66); // 0x 66 6d 74 20 BE
            binaryWriter.Write(18);
            binaryWriter.Write((short) 3); // float
            binaryWriter.Write((short) clip.channels);
            binaryWriter.Write(clip.frequency);
            binaryWriter.Write(clip.frequency * clip.channels * 4);
            binaryWriter.Write((short) (clip.channels * 4));
            binaryWriter.Write((short) 32);
            binaryWriter.Write((short) 0);
            
            binaryWriter.Write(0x61746164); // 0x 64 61 74 61 BE
            binaryWriter.Write(totalData);

            var data = new Il2CppStructArray<float>(clip.samples * clip.channels);
            if (clip.loadType != AudioClipLoadType.DecompressOnLoad && clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();
            
            clip.GetData(data, 0);
            
            foreach (var f in data) 
                binaryWriter.Write(f);
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