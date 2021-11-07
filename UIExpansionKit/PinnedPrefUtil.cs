using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.Components;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIExpansionKit
{
    internal static class PinnedPrefUtil
    {
        internal static bool CreatePinnedPrefButton(MelonPreferences_Entry entry, Transform expandoRoot, PreloadedBundleContents bundle)
        {
            switch (entry)
            {
                case MelonPreferences_Entry<bool> boolEntry:
                    CreatePinnedPrefButton(boolEntry, expandoRoot, bundle.QuickMenuToggle);
                    return true;
                case MelonPreferences_Entry<string> stringEntry:
                    if (ExpansionKitApi.EnumSettings.TryGetValue((stringEntry.Category.Identifier, stringEntry.Identifier), out var possibleValues))
                    {
                        CreatePinnedPrefButtonForString(stringEntry, possibleValues, expandoRoot, bundle.QuickMenuButton);
                        return true;
                    }

                    break;
            }

            var entryType = entry.GetReflectedType();
            if (entryType.IsEnum)
            {
                typeof(PinnedPrefUtil).GetMethod(nameof(CreatePinnedPrefButtonForEnum), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(entryType)
                    .Invoke(null, new object[] { entry, expandoRoot, bundle.QuickMenuButton });

                return true;
            }

            return false;
        }

        private static void CreatePinnedPrefButtonForString(MelonPreferences_Entry<string> stringEntry, IList<(string SettingsValue, string DisplayName)> possibleValues, Transform expandoRoot, GameObject buttonPrefab)
        {
            var button = Object.Instantiate(buttonPrefab, expandoRoot, false);
            var buttonText = button.GetComponentInChildren<Text>();
            var buttonPrefix = (stringEntry.DisplayName ?? stringEntry.Identifier) + ": ";
            
            buttonText.resizeTextMinSize = 8;
            buttonText.resizeTextMaxSize = buttonText.fontSize;
            buttonText.resizeTextForBestFit = true;
            buttonText.verticalOverflow = VerticalWrapMode.Truncate;

            void UpdateText()
            {
                buttonText.text = buttonPrefix +
                                  (possibleValues.FirstOrDefault(it => it.SettingsValue == stringEntry.Value)
                                      .DisplayName ?? stringEntry.Value ?? "");
            }
            UpdateText();

            button.GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                var currentValueIsWrong = possibleValues.All(it => it.SettingsValue != stringEntry.Value);
                var maxRows = Math.Min(possibleValues.Count + 2 + (currentValueIsWrong ? 1 : 0), 8);
                
                var menu = ExpansionKitApi.CreateCustomQmExpandoPage(LayoutDescription.WideSlimList.With(numRows: maxRows));

                if (currentValueIsWrong)
                {
                    menu.AddSimpleButton(stringEntry.Value, () =>
                    {
                        // this is the current value, so do nothing
                        menu.Hide();
                    });
                }
                
                foreach (var possibleValue in possibleValues)
                {
                    var settingValue = possibleValue.SettingsValue;
                    menu.AddSimpleButton(possibleValue.DisplayName, () =>
                    {
                        stringEntry.Value = settingValue;
                        MelonPreferences.Save();
                        menu.Hide();
                    });
                }

                menu.AddSpacer();
                menu.AddSimpleButton("Cancel", menu.Hide);
                
                menu.Show(true);
            }));
            
            Action<string, string> handler = (_, _) => { UpdateText(); };
            stringEntry.OnValueChanged += handler;
            button.GetOrAddComponent<DestroyListener>().OnDestroyed += () => stringEntry.OnValueChanged -= handler;
        }

        private static void CreatePinnedPrefButtonForEnum<T>(MelonPreferences_Entry<T> enumEntry, Transform expandoRoot, GameObject buttonPrefab) where T: Enum
        {
            var possibleValues = EnumPrefUtil.GetEnumSettingOptions<T>();
            
            var button = Object.Instantiate(buttonPrefab, expandoRoot, false);
            var buttonText = button.GetComponentInChildren<Text>();
            var buttonPrefix = (enumEntry.DisplayName ?? enumEntry.Identifier) + ": ";

            buttonText.resizeTextMinSize = 8;
            buttonText.resizeTextMaxSize = buttonText.fontSize;
            buttonText.resizeTextForBestFit = true;

            void UpdateText()
            {
                buttonText.text = buttonPrefix + possibleValues
                    .Single(it => it.SettingsValue.CompareTo(enumEntry.Value) == 0).DisplayName;
            }
            UpdateText();

            var maxRows = Math.Min(possibleValues.Count + 2, 8);

            button.GetComponent<Button>().onClick.AddListener(new Action(() =>
            {
                var menu = ExpansionKitApi.CreateCustomQmExpandoPage(LayoutDescription.WideSlimList.With(numRows: maxRows));
                
                foreach (var possibleValue in possibleValues)
                {
                    var settingValue = possibleValue.SettingsValue;
                    menu.AddSimpleButton(possibleValue.DisplayName, () =>
                    {
                        enumEntry.Value = settingValue;
                        MelonPreferences.Save();
                        menu.Hide();
                    });
                }
                
                menu.AddSpacer();
                menu.AddSimpleButton("Cancel", menu.Hide);
                
                menu.Show(true);
            }));
            
            Action<T, T> handler = (_, _) => { UpdateText(); };
            enumEntry.OnValueChanged += handler;
            button.GetOrAddComponent<DestroyListener>().OnDestroyed += () => enumEntry.OnValueChanged -= handler;
        }

        private static void CreatePinnedPrefButton(MelonPreferences_Entry<bool> boolEntry, Transform expandoRoot, GameObject toggleButtonPrefab)
        {
            var toggleButton = Object.Instantiate(toggleButtonPrefab, expandoRoot, false);
            toggleButton.GetComponentInChildren<Text>().text = boolEntry.DisplayName ?? boolEntry.Identifier;
            var toggle = toggleButton.GetComponent<Toggle>();
            toggle.isOn = boolEntry.Value;
            toggle.onValueChanged.AddListener(new Action<bool>(isOn =>
            {
                if (boolEntry.Value != isOn)
                {
                    boolEntry.Value = isOn;
                    MelonPreferences.Save();
                }
            }));
            Action<bool, bool> handler = (_, newValue) =>
            {
                toggle.isOn = newValue;
            };
            boolEntry.OnValueChanged += handler;
            toggleButton.GetOrAddComponent<DestroyListener>().OnDestroyed += () =>
            {
                boolEntry.OnValueChanged -= handler;
            };
        }
    }
}