using MelonLoader;
using Styletor.Utils;
using UnityEngine;

namespace Styletor.Styles
{
    public class ColorizerManager
    {
        public string MenuColorBase { get; private set; } = "";
        public string MenuColorHighlight { get; private set; } = "";
        public string MenuColorBackground { get; private set; } = "";
        public string MenuColorDarklight { get; private set; } = "";
        
        public string MenuColorText { get; private set; } = "";
        public string MenuColorTextHigh { get; private set; } = "";
        
        public string MenuColorAccent { get; private set; } = "";
        public string MenuColorAccentDarker { get; private set; } = "";

        public ColorizerManager(SettingsHolder settingsHolder)
        {
            var baseColorEntry = settingsHolder.BaseColorEntry;
            var accentColorEntry = settingsHolder.AccentColorEntry;
            var textColorEntry = settingsHolder.TextColorEntry;
            
            baseColorEntry.OnValueChanged += (_, _) => { UpdateColors(baseColorEntry, accentColorEntry, textColorEntry); };
            accentColorEntry.OnValueChanged += (_, _) => { UpdateColors(baseColorEntry, accentColorEntry, textColorEntry); };
            UpdateColors(baseColorEntry, accentColorEntry, textColorEntry);
        }

        private static int ParseComponent(string[] split, int idx, int defaultValue = 255)
        {
            if (split.Length <= idx || !int.TryParse(split[idx], out var parsed)) parsed = defaultValue;
            if (parsed < 0) parsed = 0;
            else if (parsed > 255) parsed = 255;
            return parsed;
        }

        private static Color ParseColor(string str)
        {
            var split = str.Split(' ');
            var r = ParseComponent(split, 0, 200);
            var g = ParseComponent(split, 1, 200);
            var b = ParseComponent(split, 2, 200);
            var a = ParseComponent(split, 3, 255);

            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        private void UpdateColors(MelonPreferences_Entry<string> baseColorEntry, MelonPreferences_Entry<string> accentColorEntry, MelonPreferences_Entry<string> textColorEntry)
        {
            var textValue = textColorEntry.Value.Trim().Length == 0 ? accentColorEntry.Value : textColorEntry.Value;
            UpdateColors(ParseColor(baseColorEntry.Value), ParseColor(accentColorEntry.Value), ParseColor(textValue));
        }

        public string ReplacePlaceholders(string input)
        {
            return input
                    .Replace("$BASE$", MenuColorBase)
                    .Replace("$HIGH$", MenuColorHighlight)
                    .Replace("$BG$", MenuColorBackground)
                    .Replace("$DARK$", MenuColorDarklight)
                    .Replace("$TEXT$", MenuColorText)
                    .Replace("$TEXTHI$", MenuColorTextHigh)
                    .Replace("$ACCT$", MenuColorAccent)
                    .Replace("$ACCDK$", MenuColorAccentDarker)
                ;
        }

        public void UpdateColors(Color @base, Color accent, Color text)
        {
            var highlight = @base.RGBMultipliedClamped(1.1f);
            var background = @base.RGBMultipliedClamped(0.9f);
            var dark = @base.RGBMultipliedClamped(0.5f);

            MenuColorBase = ColorToHex(@base);
            MenuColorHighlight = ColorToHex(highlight);
            MenuColorBackground = ColorToHex(background);
            MenuColorDarklight = ColorToHex(dark);
            
            MenuColorText = ColorToHex(text.RGBMultipliedClamped(0.9f));
            MenuColorTextHigh = ColorToHex(text);

            MenuColorAccent = ColorToHex(accent);
            MenuColorAccentDarker = ColorToHex(accent.RGBMultipliedClamped(0.7f));
        }

        private static string PartToHex(float f) => ((int)(f * 255)).ToString("x2");
        private static string ColorToHex(Color c) => $"#{PartToHex(c.r)}{PartToHex(c.g)}{PartToHex(c.b)}";
    }
}