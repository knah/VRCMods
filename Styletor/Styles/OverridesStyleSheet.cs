using System;
using System.Collections.Generic;
using System.Text;
using MelonLoader;
using Styletor.Utils;
using VRC.UI.Core.Styles;

namespace Styletor.Styles
{
    public class OverridesStyleSheet
    {
        private readonly Dictionary<string, (bool IsNew, string Text)> myStyleOverrides = new();
        private readonly Dictionary<string, (string SelectorFrom, Selector SelectorTo)> myCopies = new();
        private readonly StyleEngineWrapper myStyleEngine;

        public readonly string Name;

        public OverridesStyleSheet(string name, StyleEngineWrapper styleEngine)
        {
            Name = name;
            myStyleEngine = styleEngine;
        }

        public static OverridesStyleSheet ParseFrom(StyleEngineWrapper styleEngine, string name, IEnumerable<string> lines)
        {
            var result = new OverridesStyleSheet(name, styleEngine);
            
            var inComment = false;
            var inBody = false;
            var lastSelectorText = "";
            var bodyText = new StringBuilder();
            var lineNumber = 0;
            var nextStyleNew = false;
            var allStylesNew = false;
            foreach (var lineRaw in lines)
            {
                lineNumber++;
                var line = lineRaw.Trim();
                if (line.Length == 0)
                    continue;

                if (inComment)
                {
                    if (line.EndsWith("*/"))
                        inComment = false;
                    else if (line.Contains("*/"))
                        throw new ArgumentException($"Multi-line comments mixed into line are not supported (at line {lineNumber})");
                    
                    continue;
                }
                
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("/*"))
                {
                    inComment = true;
                    continue;
                } else if (line.Contains("/*"))
                    throw new ArgumentException($"Multi-line comments mixed into line are not supported (at line {lineNumber})");

                if (inBody)
                {
                    if (line == "}")
                    {
                        inBody = false;
                        result.ParseOverride(lastSelectorText, nextStyleNew || allStylesNew, bodyText.ToString());
                        nextStyleNew = false;
                        lastSelectorText = "";
                        bodyText.Clear();
                    } else if (line.Contains("}"))
                    {
                        throw new ArgumentException($"Mid-line closing braces are not supported (at line {lineNumber})");
                    }
                    else
                    {
                        bodyText.AppendLine(line);
                    }
                }
                else
                {
                    if (line.StartsWith("!!styletor-copy"))
                    {
                        var parts = line.Substring("!!styletor-copy".Length).Split(new[] {"!!"}, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length != 2) 
                            throw new ArgumentException($"Copy directive has too many !! in it (at line {lineNumber})");
                        var fromSelector = ParseSelector(parts[0]);
                        var toSelector = ParseSelector(parts[1]);

                        result.myCopies[toSelector.ToStringNormalized()] = (fromSelector.ToStringNormalized(), toSelector);
                        continue;
                    }

                    if (line.StartsWith("!!styletor-new"))
                    {
                        nextStyleNew = true;
                        continue;
                    }

                    if (line.StartsWith("!!styletor-new-block-start"))
                    {
                        allStylesNew = true;
                        continue;
                    }

                    if (line.StartsWith("!!styletor-new-block-end"))
                    {
                        allStylesNew = false;
                        continue;
                    }

                    var openBraceIndex = line.IndexOf('{');
                    if (openBraceIndex != -1 && openBraceIndex != line.Length - 1)
                        throw new ArgumentException($"Mid-line opening braces are not supported (at line {lineNumber})");
                    if (line.Length > 1 && openBraceIndex != 0)
                        lastSelectorText = openBraceIndex == -1 ? line : line.Substring(0, openBraceIndex);
                    inBody = openBraceIndex >= 0;
                }
            }

            return result;
        }

        public void ApplyOverrides(ColorizerManager colorizer)
        {
            foreach (var keyValuePair in myCopies)
            {
                var baseStyles = myStyleEngine.TryGetBySelector(keyValuePair.Value.SelectorFrom);
                var targetNormalizedSelector = keyValuePair.Value.SelectorTo.ToStringNormalized();
                if (baseStyles == null || baseStyles.Count == 0)
                {
                    StyletorMod.Instance.Logger.Msg($"Selector {keyValuePair.Value.SelectorFrom} not found in default style to copy into {targetNormalizedSelector}");
                    continue;
                }

                var overrideTargets = myStyleEngine.TryGetBySelector(targetNormalizedSelector);
                if (overrideTargets != null && overrideTargets.Count != 0)
                {
                    foreach (var style in baseStyles)
                    foreach (var newStylePair in style.field_Public_Dictionary_2_Int32_PropertyValue_0)
                    foreach (var overrideTarget in overrideTargets)
                        overrideTarget.field_Public_Dictionary_2_Int32_PropertyValue_0[newStylePair.Key] = newStylePair.Value;
                }
                else
                {
                    var newStyle = new ElementStyle
                    {
                        field_Public_Selector_0 = keyValuePair.Value.SelectorTo,
                        field_Public_UInt64_0 = baseStyles[0].field_Public_UInt64_0
                    };

                    // todo: is this necessary?
                    myStyleEngine.StyleEngine.Method_Public_Void_ElementStyle_String_0(newStyle, "");
                
                    foreach (var baseStyle in baseStyles)
                    foreach (var valuePair in baseStyle.field_Public_Dictionary_2_Int32_PropertyValue_0)
                        newStyle.field_Public_Dictionary_2_Int32_PropertyValue_0[valuePair.Key] = valuePair.Value;
                    
                    myStyleEngine.StyleEngine.field_Private_List_1_ElementStyle_0.Add(newStyle);
                    newStyle.field_Public_Int32_0 = myStyleEngine.StyleEngine.field_Private_List_1_ElementStyle_0.Count;
                    myStyleEngine.RegisterAddedStyle(newStyle);
                }
            }
            
            foreach (var keyValuePair in myStyleOverrides)
            {
                var baseStyles = myStyleEngine.TryGetBySelector(keyValuePair.Key);
                if (baseStyles == null && !keyValuePair.Value.IsNew)
                {
                    StyletorMod.Instance.Logger.Msg($"Selector {keyValuePair.Key} overrides nothing in default style and is not marked as new (see README)");
                    continue;
                }
                
                var style = new ElementStyle();
                myStyleEngine.StyleEngine.Method_Public_Void_ElementStyle_String_0(style, colorizer.ReplacePlaceholders(keyValuePair.Value.Text));

                if (baseStyles != null)
                {
                    foreach (var newStylePair in style.field_Public_Dictionary_2_Int32_PropertyValue_0)
                    foreach (var baseStyle in baseStyles)
                        baseStyle.field_Public_Dictionary_2_Int32_PropertyValue_0[newStylePair.Key] = newStylePair.Value;
                }
                else
                {
                    myStyleEngine.StyleEngine.field_Private_List_1_ElementStyle_0.Add(style);
                    myStyleEngine.RegisterAddedStyle(style);
                }
            }
            
            StyletorMod.Instance.Logger.Msg($"Applied {myStyleOverrides.Count} overrides from {Name}");
        }

        private static Selector ParseSelector(string selectorText) => Selector.Method_Public_Static_Selector_String_0(selectorText.Trim());

        private void ParseOverride(string lastSelectorText, bool isNew, string bodyText)
        {
            try
            {
                var selector = ParseSelector(lastSelectorText);

                var selectorNormalized = selector.ToStringNormalized();

                if (myStyleOverrides.ContainsKey(selectorNormalized))
                    StyletorMod.Instance.Logger.Warning($"Style sheet override {Name} contains duplicate selector {selectorNormalized}");

                myStyleOverrides[selectorNormalized] = (isNew, bodyText);
            }
            catch (Exception ex)
            {
                StyletorMod.Instance.Logger.Warning($"Error while parsing override style {Name}: {ex}");
            }
        }
    }
}