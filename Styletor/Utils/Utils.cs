using System;
using Il2CppSystem.IO;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Stream = System.IO.Stream;

#nullable enable

namespace Styletor.Utils
{
    public static class Utils
    {
        private delegate byte LoadTextureDelegate(IntPtr texturePtr, IntPtr arrayPtr, byte makeNonReadable);
        private delegate IntPtr EncodeAsPngDelegate(IntPtr texturePtr);

        private static readonly LoadTextureDelegate ourLoadTextureDelegate = IL2CPP.ResolveICall<LoadTextureDelegate>("UnityEngine.ImageConversion::LoadImage");
        private static readonly EncodeAsPngDelegate ourEncodeAsPng = IL2CPP.ResolveICall<EncodeAsPngDelegate>("UnityEngine.ImageConversion::EncodeToPNG");
        
        public static Il2CppStructArray<byte>? EncodeAsPng(this Texture2D texture)
        {
            var arrayPtr = ourEncodeAsPng(texture.Pointer);
            if (arrayPtr == IntPtr.Zero) return null;
            return new Il2CppStructArray<byte>(arrayPtr);
        }
        
        public static Texture2D? LoadTexture(string filePath)
        {
            return LoadTexture(File.ReadAllBytes(filePath));
        }
        
        public static Texture2D? LoadTexture(Stream stream)
        {
            return LoadTexture(stream.ReadAllBytes());
        }

        public static Texture2D? LoadTexture(Il2CppStructArray<byte> bytes)
        {
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, true);
            var success = ourLoadTextureDelegate(texture.Pointer, bytes.Pointer, 1);
            if (success == 0)
            {
                Object.Destroy(texture);
                return null;
            }

            return texture;
        }
    }
}