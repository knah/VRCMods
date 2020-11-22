using System.Collections;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace UIExpansionKit
{
    public class PreloadedBundleContents
    {
        public readonly GameObject QuickMenuExpando;
        public readonly GameObject BigMenuExpando;
        public readonly GameObject SettingsMenuExpando;
        public readonly GameObject GenericPopupWindow;
        
        public readonly GameObject QuickMenuButton;
        public readonly GameObject QuickMenuToggle;
        public readonly GameObject Label;

        public readonly GameObject SettingsCategory;
        public readonly GameObject SettingsBool;
        public readonly GameObject SettingsText;
        public readonly GameObject SettingsComboBox;

        public readonly GameObject StoredThingsParent;

        public readonly GameObject EmptyGameObjectWithRectTransform;
        
        public PreloadedBundleContents(AssetBundle bundle)
        {
            StoredThingsParent = new GameObject("ModUiPreloadedBundleContents");
            Object.DontDestroyOnLoad(StoredThingsParent);
            StoredThingsParent.SetActive(false);
            
            GameObject Load(string str)
            {
                var objectFromBundle = bundle.LoadAsset_Internal(str, Il2CppType.Of<GameObject>()).Cast<GameObject>();
                var newObject = Object.Instantiate(objectFromBundle, StoredThingsParent.transform);
                newObject.SetActive(true);
                return newObject.NoUnload();
            }

            BigMenuExpando = Load("Assets/ModUI/BigMenuSideExpando.prefab");
            SettingsMenuExpando = Load("Assets/ModUI/ModSettingsTopExpando.prefab");
            QuickMenuExpando = Load("Assets/ModUI/QuickMenuExpandoRoot.prefab");
            GenericPopupWindow = Load("Assets/ModUI/GenericPopupWindow.prefab");
            
            QuickMenuButton = Load("Assets/ModUI/BigMenuSideButton.prefab");
            QuickMenuToggle = Load("Assets/ModUI/ToggleButton.prefab");
            Label = Load("Assets/ModUI/LabelElement.prefab");
            
            SettingsCategory = Load("Assets/ModUI/CategoryElement.prefab");
            SettingsBool = Load("Assets/ModUI/CheckboxGroup.prefab");
            SettingsText = Load("Assets/ModUI/TextInputGroup.prefab");
            SettingsComboBox = Load("Assets/ModUI/ComboBoxInputGroup.prefab");

            EmptyGameObjectWithRectTransform = new GameObject("EmptyGameObjectWithRectTransform", new[] {Il2CppType.Of<RectTransform>()});
            EmptyGameObjectWithRectTransform.transform.SetParent(StoredThingsParent.transform);
        }
    }
}