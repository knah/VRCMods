using System;
using System.Linq;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit
{
    internal static class ScanningReflectionCache
    {
        internal delegate void ShowUiInputPopupAction(string title, string initialText, InputField.InputType inputType,
            bool isNumeric, string confirmButtonText, Il2CppSystem.Action<string, List<KeyCode>, Text> onComplete,
            Il2CppSystem.Action onCancel, string placeholderText = "Enter text...", bool closeAfterInput = true,
            Il2CppSystem.Action<VRCUiPopup> onPopupShown = null);

        private static ShowUiInputPopupAction ourShowUiInputPopupAction;

        internal static ShowUiInputPopupAction ShowUiInputPopup
        {
            get
            {
                if (ourShowUiInputPopupAction != null) return ourShowUiInputPopupAction;

                var targetMethod = typeof(VRCUiPopupManager).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Single(it => it.GetParameters().Length == 10 && XrefScanner.XrefScan(it).Any(jt => jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "UserInterface/MenuContent/Popups/InputPopup"));

                ourShowUiInputPopupAction = (ShowUiInputPopupAction) Delegate.CreateDelegate(typeof(ShowUiInputPopupAction), VRCUiPopupManager.prop_VRCUiPopupManager_0, targetMethod);

                return ourShowUiInputPopupAction;
            }
        }

    }
}