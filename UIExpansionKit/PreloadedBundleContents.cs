using System.Collections;
using UnityEngine;

namespace UIExpansionKit
{
    public class PreloadedBundleContents
    {
        public GameObject QuickMenuExpando;
        public GameObject BigMenuExpando;
        public GameObject SettingsMenuExpando;
        
        public GameObject QuickMenuButton;
        public GameObject QuickMenuToggle;

        public GameObject SettingsCategory;
        public GameObject SettingsBool;
        public GameObject SettingsText;

        public readonly GameObject StoredThingsParent;
        
        public PreloadedBundleContents()
        {
            StoredThingsParent = new GameObject("ModUiPreloadedBundleContents");
            Object.DontDestroyOnLoad(StoredThingsParent);
            StoredThingsParent.SetActive(false);
        }

        internal IEnumerator LoadThingsCoroutine(AssetBundle bundle)
        {
            IEnumerable Load(string str)
            {
                var assetBundleRequest = bundle.LoadAssetAsync(str, GameObject.Il2CppType);
                while (!assetBundleRequest.isDone)
                    yield return null;
                var objectFromBundle = assetBundleRequest.asset.Cast<GameObject>();
                var newObject = Object.Instantiate(objectFromBundle, StoredThingsParent.transform);
                newObject.SetActive(true);
                yield return newObject.NoUnload();
            }

            foreach (var loadRes in Load("Assets/ModUI/BigMenuSideExpando.prefab"))
                if (loadRes == null) yield return null;
                else BigMenuExpando = (GameObject) loadRes;

            foreach (var loadRes in Load("Assets/ModUI/ModSettingsTopExpando.prefab"))
                if (loadRes == null) yield return null;
                else SettingsMenuExpando = (GameObject) loadRes;
            
            foreach (var loadRes in Load("Assets/ModUI/QuickMenuExpandoRoot.prefab"))
                if (loadRes == null) yield return null;
                else QuickMenuExpando = (GameObject) loadRes;
            
            
            foreach (var loadRes in Load("Assets/ModUI/BigMenuSideButton.prefab"))
                if (loadRes == null) yield return null;
                else QuickMenuButton = (GameObject) loadRes;
            
            foreach (var loadRes in Load("Assets/ModUI/ToggleButton.prefab"))
                if (loadRes == null) yield return null;
                else QuickMenuToggle = (GameObject) loadRes;
            
            
            foreach (var loadRes in Load("Assets/ModUI/CategoryElement.prefab"))
                if (loadRes == null) yield return null;
                else SettingsCategory = (GameObject) loadRes;
            
            foreach (var loadRes in Load("Assets/ModUI/CheckboxGroup.prefab"))
                if (loadRes == null) yield return null;
                else SettingsBool = (GameObject) loadRes;
            
            foreach (var loadRes in Load("Assets/ModUI/TextInputGroup.prefab"))
                if (loadRes == null) yield return null;
                else SettingsText = (GameObject) loadRes;
        }
    }
}