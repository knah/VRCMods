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
            Il2CppSystem.Action<VRCUiPopup> onPopupShown = null, bool bUnknown = false, int charLimit = 0);

        private static ShowUiInputPopupAction ourShowUiInputPopupAction;

        internal static ShowUiInputPopupAction ShowUiInputPopup
        {
            get
            {
                if (ourShowUiInputPopupAction != null) return ourShowUiInputPopupAction;

                var candidates = typeof(VRCUiPopupManager)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(it =>
                        it.Name.StartsWith("Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_")
                        && !it.Name.EndsWith("_PDM"))
                    .ToList();

                var targetMethod = candidates.SingleOrDefault(it => XrefScanner.XrefScan(it).Any(jt =>
                    jt.Type == XrefType.Global &&
                    jt.ReadAsObject()?.ToString() == "UserInterface/MenuContent/Popups/InputPopup"));
                
                if (targetMethod == null) 
                    targetMethod = typeof(VRCUiPopupManager).GetMethod(nameof(VRCUiPopupManager.Method_Public_Void_String_String_InputType_Boolean_String_Action_3_String_List_1_KeyCode_Text_Action_String_Boolean_Action_1_VRCUiPopup_Boolean_Int32_0),
                    BindingFlags.Instance | BindingFlags.Public);

                ourShowUiInputPopupAction = (ShowUiInputPopupAction) Delegate.CreateDelegate(typeof(ShowUiInputPopupAction), VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0, targetMethod);

                return ourShowUiInputPopupAction;
            }
        }

    }
}