using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIExpansionKit
{
    public static class ModSettingsHandler
    {
        private static PreloadedBundleContents ourStuffBundle;
        private static readonly Dictionary<string, bool> ourCategoryExpanded = new Dictionary<string, bool>(); 

        public static void Initialize(PreloadedBundleContents stuffBundle)
        {
            ourStuffBundle = stuffBundle;
        }

        public static IEnumerator PopulateSettingsPanel(RectTransform settingsContentRoot)
        {
            yield return null;
            yield return null;
            yield return null;
            
            var categoryPrefab = ourStuffBundle.SettingsCategory;
            var boolPrefab = ourStuffBundle.SettingsBool;
            var textPrefab = ourStuffBundle.SettingsText;
            var comboBoxPrefab = ourStuffBundle.SettingsComboBox;

            settingsContentRoot.DestroyChildren();

            var pinnedSettings = ExpansionKitSettings.ListPinnedPrefs(false).ToList();
            
            foreach (var keyValuePair in MelonPrefs.GetPreferences())
            {
                var categoryId = keyValuePair.Key;
                var prefDict = keyValuePair.Value;

                if (ExpansionKitApi.CustomCategoryUIs.TryGetValue(categoryId, out var specificPrefab))
                {
                    Object.Instantiate(specificPrefab, settingsContentRoot, false);
                    continue;
                }

                var prefsToPopulate = prefDict.Where(it => !it.Value.Hidden).ToList();
                
                if(prefsToPopulate.Count == 0)
                    continue;

                var categoryUi = Object.Instantiate(categoryPrefab, settingsContentRoot, false);
                categoryUi.GetComponentInChildren<Text>().text = MelonPrefs.GetCategoryDisplayName(categoryId);
                var categoryUiContent = categoryUi.transform.Find("CategoryEntries");
                var expandButtonTransform = categoryUi.transform.Find("ExpandButton");
                var expandButton = expandButtonTransform.GetComponent<Button>();
                var expandButtonText = expandButtonTransform.GetComponentInChildren<Text>();

                void SetExpanded(bool expanded)
                {
                    expandButtonText.text = expanded ? "^" : "V";
                    categoryUiContent.gameObject.SetActive(expanded);
                }
                
                expandButton.onClick.AddListener(new Action(() =>
                {
                    SetExpanded(ourCategoryExpanded[categoryId] = !ourCategoryExpanded[categoryId]);
                }));

                if (!ourCategoryExpanded.ContainsKey(categoryId))
                    ourCategoryExpanded[categoryId] = !ExpansionKitSettings.IsCategoriesStartCollapsed();
                
                SetExpanded(ourCategoryExpanded[categoryId]);
                
                foreach (var valuePair in prefsToPopulate)
                {
                    var prefId = valuePair.Key;
                    var prefDesc = valuePair.Value;

                    switch (prefDesc.Type)
                    {
                        case MelonPrefs.MelonPreferenceType.STRING:
                        {
                            if (ExpansionKitApi.EnumSettings.TryGetValue((categoryId, prefId), out var enumValues))
                            {
                                var comboSetting = Object.Instantiate(comboBoxPrefab, categoryUiContent, false);
                                comboSetting.GetComponentInChildren<Text>().text = prefDesc.DisplayText ?? prefId;
                                var dropdown = comboSetting.GetComponentInChildren<Dropdown>();
                                var options = new Il2CppSystem.Collections.Generic.List<Dropdown.OptionData>();
                                var currentValue = MelonPrefs.GetString(categoryId, prefId);
                                var selectedIndex = enumValues.Count;
                                for (var i = 0; i < enumValues.Count; i++)
                                {
                                    var valueTuple = enumValues[i];
                                    options.Add(new Dropdown.OptionData(valueTuple.DisplayName));
                                    if (currentValue == valueTuple.SettingsValue)
                                        selectedIndex = i;
                                }
                                if (enumValues.All(it => it.SettingsValue != currentValue)) 
                                    options.Add(new Dropdown.OptionData(currentValue));
                                dropdown.options = options;
                                dropdown.value = selectedIndex;
                                dropdown.onValueChanged.AddListener(new Action<int>(value =>
                                {
                                    prefDesc.ValueEdited = value >= enumValues.Count
                                        ? currentValue
                                        : enumValues[value].SettingsValue;
                                }));
                            }
                            else
                            {
                                var textSetting = Object.Instantiate(textPrefab, categoryUiContent, false);
                                textSetting.GetComponentInChildren<Text>().text = prefDesc.DisplayText ?? prefId;
                                var textField = textSetting.GetComponentInChildren<InputField>();
                                textField.text = MelonPrefs.GetString(categoryId, prefId);
                                textField.onValueChanged.AddListener(new Action<string>(value =>
                                {
                                    prefDesc.ValueEdited = value;
                                }));
                                textSetting.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                                {
                                    BuiltinUiUtils.ShowInputPopup(prefDesc.DisplayText ?? prefId, textField.text,
                                        InputField.InputType.Standard, false, "Done",
                                        (result, _, __) => prefDesc.ValueEdited = textField.text = result);
                                }));
                            }

                            break;
                        }
                        case MelonPrefs.MelonPreferenceType.BOOL:
                            var boolSetting = Object.Instantiate(boolPrefab, categoryUiContent, false);
                            boolSetting.GetComponentInChildren<Text>().text = prefDesc.DisplayText ?? prefId;
                            var mainToggle = boolSetting.transform.Find("Toggle").GetComponent<Toggle>();
                            mainToggle.isOn = MelonPrefs.GetBool(categoryId, prefId);
                            mainToggle.onValueChanged.AddListener(new Action<bool>(
                                isSet =>
                                {
                                    prefDesc.ValueEdited = isSet.ToString().ToLowerInvariant();
                                }));
                            var pinToggle = boolSetting.transform.Find("PinToggle").GetComponent<Toggle>();
                            pinToggle.isOn = pinnedSettings.Contains((categoryId, prefId));
                            pinToggle.onValueChanged.AddListener(new Action<bool>(isSet =>
                            {
                                if (isSet) 
                                    ExpansionKitSettings.PinPref(categoryId, prefId);
                                else
                                    ExpansionKitSettings.UnpinPref(categoryId, prefId);
                            }));
                            break;
                        case MelonPrefs.MelonPreferenceType.INT:
                        case MelonPrefs.MelonPreferenceType.FLOAT:
                        {
                            var textSetting = Object.Instantiate(textPrefab, categoryUiContent, false);
                            textSetting.GetComponentInChildren<Text>().text = prefDesc.DisplayText ?? prefId;
                            var textField = textSetting.GetComponentInChildren<InputField>();
                            textField.text = MelonPrefs.GetString(categoryId, prefId);
                            textField.contentType = prefDesc.Type == MelonPrefs.MelonPreferenceType.INT
                                ? InputField.ContentType.IntegerNumber
                                : InputField.ContentType.DecimalNumber;
                            textField.onValueChanged.AddListener(new Action<string>(value =>
                            {
                                prefDesc.ValueEdited = value;
                            }));
                            textSetting.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                                {
                                    BuiltinUiUtils.ShowInputPopup(prefDesc.DisplayText ?? prefId, textField.text,
                                        InputField.InputType.Standard, prefDesc.Type == MelonPrefs.MelonPreferenceType.INT, "Done", 
                                        (result, _, __) => prefDesc.ValueEdited = textField.text = result);
                                }));
                            break;
                        }
                        default:
                            MelonLogger.LogError($"Unknown mod pref type {prefDesc.Type}");
                            break;
                    }
                }
            }
        }
    }
}