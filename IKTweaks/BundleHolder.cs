using System.IO;
using System.Reflection;
using UnityEngine;

namespace IKTweaks
{
    public static class BundleHolder
    {
        public static AssetBundle Bundle;
        public static RuntimeAnimatorController TPoseController;
        
        public static void Init()
        {
            var memStream = new MemoryStream();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IKTweaks.iktweaks"))
            {
                stream.CopyTo(memStream);
            }
            Bundle = AssetBundle.LoadFromMemory(memStream.ToArray());
            Bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            TPoseController = Bundle.LoadAsset(Bundle.GetAllAssetNames()[0]).Cast<RuntimeAnimatorController>();
            TPoseController.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        }
    }
}