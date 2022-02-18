using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UIExpansionKit.Components;
using UIExpansionKit.FieldInject;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI.Core.Styles;

namespace UIExpansionKit
{
    public class StylingHelper
    {
        public static StringInjectedField<StyleElementWrapper> SewElementTypeIdField;
        public static StructInjectedField<StyleElementWrapper, bool> SewOnMainMenuField;
        public static bool StyletorPresent;

        public static StyleEngine StyleEngine;

        private static readonly Dictionary<string, string> ourDefaultStyleMap = new()
        {
            {"ButtonIcon", "Icon"},
            {"ToggleBackgroundIcon", "UixToggleBackgroundIcon"},
            {"Background", "BackgroundLayer1"},
            {"ExpandoBackground", "BackgroundLayer1"},
            {"ExpandoButton", "ButtonSquareQM"},
            {"ExpandoToggleButton", "ButtonSquareQM"},
            {"ExpandoToggleButtonIcon", "Icon"},
            {"ExpandoFlipButton", "ButtonSquareQM"},
            {"Button", "ButtonSquareQM"},
            {"TextInputBox", "ButtonSquareQM"},
            {"Toggle", "ButtonSquareQM"},
            {"Scrollbar", "ElementClass_Scrollbar_BackgroundImage"},
            {"ScrollbarHandle", "ElementClass_Scrollbar_HandleImage"},
            {"Dropdown", "WingDropdown"},
            {"DropdownBackground", "WingDropdown"},
            {"DropdownItemBackground", "WingDropdownItemBackground"},
            {"TextBig", "H1"},
            {"Text", "H3"},
            {"TextPlaceholder", "H4"},
            {"Dummy", ""},
            
        };

        internal static void Init()
        {
            SewElementTypeIdField = new("ElementTypeId");
            SewOnMainMenuField = new("OnMainMenu");
            
            StyletorPresent = MelonHandler.Mods.Any(it => it.Info.Name == "Styletor");
        }

        public static void AddStyleElement(Component comp, string elementClass, string elementTag = "") => AddStyleElement(comp.gameObject, elementClass, elementTag);

        public static void AddStyleElement(GameObject go, string elementClass, string elementTag = "")
        {
            var styleElement = go.GetOrAddComponent<StyleElement>();
            styleElement.field_Public_String_0 = elementTag;
            styleElement.field_Public_String_1 = elementClass;

            var image = go.GetComponent<Image>();
            if (image != null) 
                image.pixelsPerUnitMultiplier = 3; // todo: rebuild bundles for new PPU values?

            var selectable = go.GetComponent<Selectable>();
            if (selectable != null)
                selectable.colors = new ColorBlock
                {
                    colorMultiplier = 1,
                    disabledColor = Color.white,
                    normalColor = Color.white,
                    highlightedColor = Color.white,
                    pressedColor = Color.white,
                    selectedColor = Color.white,
                };

            styleElement.Method_Public_Void_StyleEngine_0(StyleEngine);
            styleElement.Method_Protected_Void_0();
        }

        public static void ApplyStyling(StyleElementWrapper wrapper)
        {
            var requestedStyle = SewElementTypeIdField.Get(wrapper);
            if (string.IsNullOrEmpty(requestedStyle))
            {
                MelonLogger.Error("Empty requested style on SEW");
                return;
            }

            if (!ourDefaultStyleMap.TryGetValue(requestedStyle, out var className))
            {
                MelonLogger.Error($"Unknown requested style: {requestedStyle}");
                return;
            }

            if (wrapper.AdditionalClass != null)
                className += " " + wrapper.AdditionalClass;
            
            AddStyleElement(wrapper, className);
        }
    }
}