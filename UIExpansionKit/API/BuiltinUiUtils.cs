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
            string confirmButtonText, Action<string, List<KeyCode>, Text> onComplete, Action onCancel,
            string placeholderText, bool closeAfterInput, Action<VRCUiPopup> onPopupShown)
        {
            ScanningReflectionCache.ShowUiInputPopup(title, initialText, inputType, isNumeric, confirmButtonText, onComplete, onCancel, placeholderText, closeAfterInput, onPopupShown);
        }
        
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
        /// <param name="showLimitLabel">If true, text length limit label will be shown</param>
        /// <param name="textLengthLimit">Maximum text length</param>
        public static void ShowInputPopup(string title, string initialText, InputField.InputType inputType, bool isNumeric,
            string confirmButtonText, Action<string, List<KeyCode>, Text> onComplete, Action onCancel = null,
            string placeholderText = "Enter text...", bool closeAfterInput = true, Action<VRCUiPopup> onPopupShown = null,
            bool showLimitLabel = false, int textLengthLimit = 0)
        {
            ScanningReflectionCache.ShowUiInputPopup(title, initialText, inputType, isNumeric, confirmButtonText, onComplete, onCancel, placeholderText, closeAfterInput, onPopupShown, showLimitLabel, textLengthLimit);
        }
        
        public static event Action QuickMenuClosed;
        public static event Action FullMenuClosed;
        public static event Action<ExpandedMenu> OnMenuOpened;

        internal static void InvokeQuickMenuClosed() => QuickMenuClosed?.Invoke();
        internal static void InvokeFullMenuClosed() => FullMenuClosed?.Invoke();
        internal static void InvokeMenuOpened(ExpandedMenu menu) => OnMenuOpened?.Invoke(menu);
    }
}