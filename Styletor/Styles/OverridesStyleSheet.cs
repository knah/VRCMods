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
        private readonly Dictionary<string, string> myStyleOverrides = new();
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
            int lineNumber = 0;
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
                        result.ParseOverride(lastSelectorText, bodyText.ToString());
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
            foreach (var keyValuePair in myStyleOverrides)
            {
                var baseStyles = myStyleEngine.TryGetBySelector(keyValuePair.Key);
                if (baseStyles == null)
                {
                    MelonLogger.Msg($"Selector {keyValuePair.Key} overrides nothing in default style");
                    continue;
                }
                
                var style = new ElementStyle();
                myStyleEngine.StyleEngine.Method_Public_Void_ElementStyle_String_0(style, colorizer.ReplacePlaceholders(keyValuePair.Value));
                
                foreach (var newStylePair in style.field_Public_Dictionary_2_Int32_PropertyValue_0)
                foreach (var baseStyle in baseStyles)
                    baseStyle.field_Public_Dictionary_2_Int32_PropertyValue_0[newStylePair.Key] = newStylePair.Value;
            }
            
            MelonLogger.Msg($"Applies {myStyleOverrides.Count} overrides");
        }

        private void ParseOverride(string lastSelectorText, string bodyText)
        {
            try
            {
                var selector = Selector.Method_Public_Static_Selector_String_0(lastSelectorText);

                var selectorNormalized = selector.ToStringNormalized();

                if (myStyleOverrides.ContainsKey(selectorNormalized))
                    MelonLogger.Warning($"Style sheet override {Name} contains duplicate selector {selectorNormalized}");

                myStyleOverrides[selectorNormalized] = bodyText;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error while parsing override style {Name}: {ex}");
            }
        }
    }
}