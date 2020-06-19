using System;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit.API
{
    public static class BuiltinUiUtils
    {
        /// <summary>
        /// Shows a VRC keyboard input popup. Requires a big menu page to be open.
        /// </summary>
        /// <param name="title">Text displayed in popup header</param>
        /// <param name="initialText">Initial text in the input</param>
        /// <param name="inputType">The input type</param>
        /// <param name="isNumeric">true means a numeric keypad</param>
        /// <param name="onComplete">Invoked when the user clicks the confirm button</param>
        /// <param name="confirmButtonText">The text on confirmation button</param>
        /// <param name="onCancel">Invoked if user cancels text input</param>
        /// <param name="placeholderText">Text show in the input field if no text is already there</param>
        /// <param name="closeAfterInput">Keep this set to true or the universe will implode</param>
        /// <param name="onPopupShown">Invoked with the instance of input popup shown</param>
        public static void ShowInputPopup(string title, string initialText, InputField.InputType inputType, bool isNumeric,
            string confirmButtonText, Action<string, List<KeyCode>, Text> onComplete, Action onCancel = null,
            string placeholderText = "Enter text...", bool closeAfterInput = true, Action<VRCUiPopup> onPopupShown = null)
        {
            ScanningReflectionCache.ShowUiInputPopup(title, initialText, inputType, isNumeric, confirmButtonText, onComplete, onCancel, placeholderText, closeAfterInput, onPopupShown);
        }
    }
}