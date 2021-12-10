using MelonLoader;
using Styletor.Utils;
using UnityEngine;

namespace Styletor.Styles
{
    public class ColorizerManager
    {
        private readonly SettingsHolder mySettings;
        public string MenuColorBase { get; private set; } = "";
        public string MenuColorHighlight { get; private set; } = "";
        public string MenuColorBackground { get; private set; } = "";
        public string MenuColorDarklight { get; private set; } = "";
        
        public string MenuColorText { get; private set; } = "";
        public string MenuColorTextHigh { get; private set; } = "";
        
        public string MenuColorAccent { get; private set; } = "";
        public string MenuColorAccentDarker { get; private set; } = "";

        public ColorizerManager(SettingsHolder settings)
        {
            mySettings = settings;
            
            settings.BaseColorEntry.OnValueChanged += (_, _) => { UpdateColors(); };
            settings.AccentColorEntry.OnValueChanged += (_, _) => { UpdateColors(); };
            settings.TextColorEntry.OnValueChanged += (_, _) => { UpdateColors(); };
            UpdateColors();
        }

        private void UpdateColors()
        {
            UpdateColors(mySettings.BaseColor, mySettings.AccentColor, mySettings.TextColor);
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