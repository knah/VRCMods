using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

            var pinnedSettings = ExpansionKitSettings.ListPinnedPrefs().ToList();
            
            foreach (var category in MelonPreferences.Categories)
            {
                var categoryId = category.Identifier;
                var prefDict = category.Entries;

                if (ExpansionKitApi.CustomCategoryUIs.TryGetValue(categoryId, out var specificPrefab))
                {
                    Object.Instantiate(specificPrefab, settingsContentRoot, false);
                    continue;
                }

                var prefsToPopulate = prefDict.Where(it => !it.IsHidden).ToList();
                
                if (prefsToPopulate.Count == 0)
                    continue;

                var categoryUi = Object.Instantiate(categoryPrefab, settingsContentRoot, false);
                categoryUi.GetComponentInChildren<Text>().text = category.DisplayName ?? categoryId;
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
                
                
                
                void CreateNumericSetting<T>(MelonPreferences_Entry<T> entry, Func<T, string> toString, Func<string, T?> fromString) where T:struct, IEquatable<T>
                {
                    var textSetting = Object.Instantiate(textPrefab, categoryUiContent, false);
                    textSetting.GetComponentInChildren<Text>().text = entry.DisplayName ?? entry.Identifier;
                    var textField = textSetting.GetComponentInChildren<InputField>();
                    textField.text = toString(entry.Value);
                    textField.contentType = typeof(T) == typeof(float) || typeof(T) == typeof(double)
                        ? InputField.ContentType.DecimalNumber
                        : InputField.ContentType.IntegerNumber;
                    textField.onValueChanged.AddListener(new Action<string>(value =>
                    {
                        var parsed = fromString(value);
                        if (parsed != null && !entry.Value.Equals(parsed.Value)) 
                            entry.Value = parsed.Value;
                    }));
                    textSetting.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                    {
                        BuiltinUiUtils.ShowInputPopup(entry.DisplayName ?? entry.Identifier, textField.text,
                            InputField.InputType.Standard, false, "Done", 
                            (result, _, __) =>
                            {
                                var parsed = fromString(result);
                                if (parsed != null)
                                {
                                    textField.text = result;
                                    if (!entry.Value.Equals(parsed.Value)) 
                                        entry.Value = parsed.Value;
                                }
                            });
                    }));
                    entry.OnValueChanged += (_, newValue) =>
                    {
                        UiExpansionKitMod.AreSettingsDirty = true;

                        textField.text = toString(newValue);
                    };
                }
                
                
                
                foreach (var pref in prefsToPopulate)
                {
                    var prefId = pref.Identifier;

                    switch (pref)
                    {
                        case MelonPreferences_Entry<string> stringPref:
                        {
                            if (ExpansionKitApi.EnumSettings.TryGetValue((categoryId, prefId), out var enumValues))
                            {
                                var comboSetting = Object.Instantiate(comboBoxPrefab, categoryUiContent, false);
                                comboSetting.GetComponentInChildren<Text>().text = pref.DisplayName ?? prefId;
                                var dropdown = comboSetting.GetComponentInChildren<Dropdown>();
                                var options = new Il2CppSystem.Collections.Generic.List<Dropdown.OptionData>();
                                var currentValue = stringPref.Value;
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
                                    stringPref.Value = value >= enumValues.Count
                                        ? currentValue
                                        : enumValues[value].SettingsValue;
                                }));
                                stringPref.OnValueChanged += (old, newValue) =>
                                {
                                    UiExpansionKitMod.AreSettingsDirty = true;
                                    
                                    int newIndex = -1;
                                    for (var i = 0; i < enumValues.Count; i++)
                                    {
                                        if (enumValues[i].SettingsValue == newValue)
                                        {
                                            newIndex = i;
                                            break;
                                        }
                                    }

                                    if (newIndex != -1)
                                        dropdown.value = selectedIndex;
                                };
                            }
                            else
                            {
                                var textSetting = Object.Instantiate(textPrefab, categoryUiContent, false);
                                textSetting.GetComponentInChildren<Text>().text = pref.DisplayName ?? prefId;
                                var textField = textSetting.GetComponentInChildren<InputField>();
                                textField.text = stringPref.Value;
                                textField.onValueChanged.AddListener(new Action<string>(value =>
                                {
                                    if (stringPref.Value != value)
                                        stringPref.Value = value;
                                }));
                                textSetting.GetComponentInChildren<Button>().onClick.AddListener(new Action(() =>
                                {
                                    BuiltinUiUtils.ShowInputPopup(pref.DisplayName ?? prefId, textField.text,
                                        InputField.InputType.Standard, false, "Done",
                                        (result, _, __) =>
                                        {
                                            textField.text = result;
                                            if (stringPref.Value != result)
                                                stringPref.Value = result;
                                        });
                                }));
                                stringPref.OnValueChanged += (_, newValue) =>
                                {
                                    UiExpansionKitMod.AreSettingsDirty = true;
                                    textField.text = newValue;
                                };
                            }

                            break;
                        }
                        case MelonPreferences_Entry<bool> boolEntry:
                            var boolSetting = Object.Instantiate(boolPrefab, categoryUiContent, false);
                            boolSetting.GetComponentInChildren<Text>().text = pref.DisplayName ?? prefId;
                            var mainToggle = boolSetting.transform.Find("Toggle").GetComponent<Toggle>();
                            mainToggle.isOn = boolEntry.Value;
                            mainToggle.onValueChanged.AddListener(new Action<bool>(
                                isSet =>
                                {
                                    if (boolEntry.Value != isSet)
                                        boolEntry.Value = isSet;
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
                            ExpansionKitSettings.PinsEntry.OnValueChanged += (_, __) =>
                            {
                                pinToggle.isOn = ExpansionKitSettings.IsPinned(categoryId, prefId);
                            };
                            boolEntry.OnValueChanged += (old, newValue) =>
                            {
                                UiExpansionKitMod.AreSettingsDirty = true;
                                
                                mainToggle.isOn = newValue;
                            };
                            break;
                        case MelonPreferences_Entry<float> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        case MelonPreferences_Entry<double> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        case MelonPreferences_Entry<byte> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => byte.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        case MelonPreferences_Entry<short> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        case MelonPreferences_Entry<int> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        case MelonPreferences_Entry<long> floatEntry:
                            CreateNumericSetting(floatEntry, f => f.ToString(CultureInfo.InvariantCulture),
                                s => long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : null);
                            break;
                        default:
                            if (MelonDebug.IsEnabled())
                                MelonLogger.Msg($"Unknown mod pref type {pref.GetType()}");
                            break;
                    }
                }
            }

            UiExpansionKitMod.SetLayerRecursively(settingsContentRoot.gameObject, 12);
        }
    }
}