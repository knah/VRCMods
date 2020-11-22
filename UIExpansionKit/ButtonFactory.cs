using UIExpansionKit.API;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit
{
    internal static class ButtonFactory
    {
        internal static void CreateButtonForRegistration(ExpansionKitApi.ButtonRegistration registration, Transform root, bool isQuickMenu)
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
                    buttonInstance.GetComponentInChildren<Text>().text = registration.Text;
                    buttonInstance.GetComponent<Button>().onClick.AddListener(registration.Action);
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                } else if (registration.ToggleAction != null)
                {
                    // todo: non-qm proper toggle
                    var clickButtonPrefab = isQuickMenu ? stuff.QuickMenuToggle : stuff.QuickMenuToggle;

                    var buttonInstance = Object.Instantiate(clickButtonPrefab, root, false);
                    buttonInstance.GetComponentInChildren<Text>().text = registration.Text;

                    var toggle = buttonInstance.GetComponent<Toggle>();
                    toggle.isOn = registration.InitialState?.Invoke() ?? false;
                    toggle.onValueChanged.AddListener(registration.ToggleAction);
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                }
                else
                {
                    var clickButtonPrefab = stuff.Label;

                    var buttonInstance = Object.Instantiate(clickButtonPrefab, root, false);
                    buttonInstance.GetComponentInChildren<Text>().text = registration.Text;
                    registration.InstanceConsumer?.Invoke(buttonInstance);
                }
            }
            else
            {
                var newObject = Object.Instantiate(UiExpansionKitMod.Instance.StuffBundle.EmptyGameObjectWithRectTransform, root, false);
                registration.InstanceConsumer?.Invoke(newObject);
            }
        }
    }
}