using System;
using MelonLoader;
using TMPro;
using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UIExpansionKit
{
    internal static class ButtonFactory
    {
        internal static void CreateButtonForRegistration(ExpansionKitApi.ButtonRegistration registration, Transform root, bool isQuickMenu)
        {
            try
            {
                CreateButtonForRegistrationImpl(registration, root, isQuickMenu);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception when creating a button for registration of {registration}: {ex}");
            }
        }
        
        private static void CreateButtonForRegistrationImpl(ExpansionKitApi.ButtonRegistration registration, Transform root, bool isQuickMenu)
        {
            if (registration.Prefab != null)
            {
                var newObject = Object.Instantiate(registration.Prefab, root, false);
                registration.InstanceConsumer?.Invoke(newObject);
            }
            else if(registration.Text != null)
            {
                var stuff = UiExpansionKitMod.Instance.StuffBundle;
                
                if (registration.Action != null)
                {
                    var clickButtonPrefab = stuff.QuickMenuButton;

                    var buttonInstance = Object.Instantiate(clickButtonPrefab, root, false);
                    var textComponent = buttonInstance.GetComponentInChildren<TMP_Text>(true);
                    textComponent.text = registration.Text;
                    var legacyTextComponent = buttonInstance.GetComponentInChildren<Text>(true);
                    legacyTextComponent.text = registration.Text;
                    buttonInstance.GetComponent<Button>().onClick.AddListener(registration.Action);
                    UnityUtils.LinkTextIntoTmp(buttonInstance);
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                } else if (registration.ToggleAction != null)
                {
                    // todo: non-qm proper toggle
                    var clickButtonPrefab = isQuickMenu ? stuff.QuickMenuToggle : stuff.QuickMenuToggle;

                    var buttonInstance = Object.Instantiate(clickButtonPrefab, root, false);
                    var textComponent = buttonInstance.GetComponentInChildren<TMP_Text>(true);
                    textComponent.text = registration.Text;
                    var legacyTextComponent = buttonInstance.GetComponentInChildren<Text>(true);
                    legacyTextComponent.text = registration.Text;

                    var toggle = buttonInstance.GetComponent<Toggle>();
                    toggle.isOn = registration.InitialState?.Invoke() ?? false;
                    toggle.onValueChanged.AddListener(registration.ToggleAction);
                    UnityUtils.LinkTextIntoTmp(buttonInstance);
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                }
                else
                {
                    var buttonInstance = Object.Instantiate(stuff.Label, root, false);
                    var textComponent = buttonInstance.GetComponentInChildren<TMP_Text>(true);
                    textComponent.text = registration.Text;
                    var legacyTextComponent = buttonInstance.GetComponentInChildren<Text>(true);
                    legacyTextComponent.text = registration.Text;
                    UnityUtils.LinkTextIntoTmp(buttonInstance);
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                }
            }
            else
            {
                var newObject = Object.Instantiate(UiExpansionKitMod.Instance.StuffBundle.EmptyGameObjectWithRectTransform, root, false);
                registration.InstanceConsumer?.Invoke(newObject);
                UnityUtils.LinkTextIntoTmp(newObject);
            }
        }
    }
}