using System.IO;
using System.Reflection;
using FavCat.CustomLists;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace FavCat
{
    public static class AssetsHandler
    {
        private static AssetBundle myAssetBundle;
        
        public static GameObject ListPrefab;
        public static GameObject PickerPrefab;
        
        public static Sprite IconPC;
        public static Sprite IconQuest;
        public static Sprite IconUni;
        
        public static Sprite PreviewLoading;
        public static Sprite PreviewError;
        
        public static void Load()
        {
            T NoUnload<T>(T obj) where T: Object
            {
                obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return obj;
            }
            
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FavCat.extraui"))
            using (var tempStream = new MemoryStream((int) stream.Length))
            {
                stream.CopyTo(tempStream);
                
                myAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                myAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            var screensRoot = GameObject.Find("UserInterface/MenuContent/Screens").transform;
            ListPrefab = Object.Instantiate(myAssetBundle.LoadAsset_Internal("Assets/ExtraUi/CustomUiList.prefab", Il2CppType.Of<GameObject>()).Cast<GameObject>(), screensRoot, false);
            ListPrefab.SetActive(false);
            
            var pickerPoolRoot = new GameObject("CustomPickerPool");
            pickerPoolRoot.transform.SetParent(screensRoot);
            
            var pickerPrefab = Object.Instantiate(myAssetBundle.LoadAsset_Internal("Assets/ExtraUi/CustomPicker.prefab", Il2CppType.Of<GameObject>()).Cast<GameObject>(), screensRoot, false);
            pickerPrefab.SetActive(false);
            PickerPrefab = pickerPrefab;

            IconPC = NoUnload(myAssetBundle.LoadAsset("Assets/ExtraUI/EUI-IconPC.png", Il2CppType.Of<Sprite>()).Cast<Sprite>());
            IconQuest = NoUnload(myAssetBundle.LoadAsset("Assets/ExtraUI/EUI-IconQ.png", Il2CppType.Of<Sprite>()).Cast<Sprite>());
            IconUni = NoUnload(myAssetBundle.LoadAsset("Assets/ExtraUI/EUI-IconU.png", Il2CppType.Of<Sprite>()).Cast<Sprite>());
            
            PreviewLoading = NoUnload(myAssetBundle.LoadAsset("Assets/ExtraUI/EUI-LoadingPreview.png", Il2CppType.Of<Sprite>()).Cast<Sprite>());
            PreviewError = NoUnload(myAssetBundle.LoadAsset("Assets/ExtraUI/EUI-ErrorPreview.png", Il2CppType.Of<Sprite>()).Cast<Sprite>());

            new PickerPool(pickerPrefab, pickerPoolRoot);
        }
    }
}