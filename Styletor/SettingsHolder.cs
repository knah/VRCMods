using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;

namespace Styletor
{
    public class SettingsHolder
    {
        internal const string CategoryIdentifier = "Styletor";
        
        internal readonly MelonPreferences_Entry<string> StyleEntry;
        internal readonly MelonPreferences_Entry<string> DisabledMixinsEntry;
        internal readonly MelonPreferences_Entry<string> BaseColorEntry;
        internal readonly MelonPreferences_Entry<string> AccentColorEntry;
        internal readonly MelonPreferences_Entry<string> TextColorEntry;
        internal readonly MelonPreferences_Entry<string> AuxColorEntry;

        internal readonly MelonPreferences_Entry<SingleColorMode> UiLasersModeEntry;
        internal readonly MelonPreferences_Entry<string> UiLasersColorEntry;
        
        internal readonly MelonPreferences_Entry<MultiColorMode> ActionMenuModeEntry;
        internal readonly MelonPreferences_Entry<string> ActionMenuBaseColorEntry;
        internal readonly MelonPreferences_Entry<string> ActionMenuAccentColorEntry;

        internal readonly List<(string SettingsValue, string DisplayName)> EnumSettingsInfo = new();
        
        public SettingsHolder()
        {
            var category = MelonPreferences.CreateCategory(CategoryIdentifier, "Styletor");
            
            DisabledMixinsEntry = category.CreateEntry("DisabledMixins", "", is_hidden: true);

            BaseColorEntry = category.CreateEntry("BaseColorString", "0 60 60", "Menu base color (red green blue; 0-255)");
            AccentColorEntry = category.CreateEntry("AccentColorString", "106 227 249", "Accent color (icons etc)");
            TextColorEntry = category.CreateEntry("TextColorString", "", "Text color (empty = use accent)");
            AuxColorEntry = category.CreateEntry("AuxColorString", "", "Auxiliary color (other things; empty = use accent)");
            
            StyleEntry = category.CreateEntry("SelectedStyle", "Styletor.BundledStyles.basic-recolorable.styletor.zip", "Selected style");
            
            UiLasersModeEntry = category.CreateEntry("LasersMode", SingleColorMode.UseAccentColor, "Laser/cursor recoloring mode");
            
            ActionMenuModeEntry = category.CreateEntry("ActionMenuMode", MultiColorMode.UseMainScheme, "Action Menu color mode");

            // todo: right default color?
            UiLasersColorEntry = category.CreateEntry("UiLasersColorString", "106 227 249", "UI Lasers/cursor color");
            
            // todo: right default color?
            ActionMenuBaseColorEntry = category.CreateEntry("ActionMenuBaseColorString", "106 227 249", "Action Menu base color");
            ActionMenuAccentColorEntry = category.CreateEntry("ActionMenuAccentColorString", "106 227 249", "Action Menu accent color");

            LinkSettingVisibility(UiLasersModeEntry, UiLasersColorEntry);
            LinkSettingVisibility(ActionMenuModeEntry, ActionMenuBaseColorEntry);
            LinkSettingVisibility(ActionMenuModeEntry, ActionMenuAccentColorEntry);

            ExpansionKitApi.RegisterSettingAsStringEnum(CategoryIdentifier, StyleEntry.Identifier, EnumSettingsInfo);
        }

        internal void RegisterUpdateDelegate(MelonPreferences_Entry<SingleColorMode> modeEntry, MelonPreferences_Entry<string> ownColorEntry, Action onUpdate)
        {
            MelonPreferences_Entry? lastSubbedEntry1 = null;
            MelonPreferences_Entry? lastSubbedEntry2 = null;

            (MelonPreferences_Entry?, MelonPreferences_Entry?) GetCurrentEntry()
            {
                return modeEntry.Value switch
                {
                    SingleColorMode.DoNotRecolor => (null, null),
                    SingleColorMode.UseBaseColor => (BaseColorEntry, null),
                    SingleColorMode.UseAccentColor => (AccentColorEntry, null),
                    SingleColorMode.UseTextColor => (TextColorEntry, AccentColorEntry),
                    SingleColorMode.UseAuxColor => (AuxColorEntry, AccentColorEntry),
                    SingleColorMode.UseOwnColor => ((MelonPreferences_Entry?, MelonPreferences_Entry?))(ownColorEntry, null),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            
            void UpdateBaseEntrySub()
            {
                if (lastSubbedEntry1 != null) lastSubbedEntry1.OnValueChangedUntyped -= onUpdate;
                if (lastSubbedEntry2 != null) lastSubbedEntry2.OnValueChangedUntyped -= onUpdate;

                (lastSubbedEntry1, lastSubbedEntry2) = GetCurrentEntry();

                if (lastSubbedEntry1 != null) lastSubbedEntry1.OnValueChangedUntyped += onUpdate;
                if (lastSubbedEntry2 != null) lastSubbedEntry2.OnValueChangedUntyped += onUpdate;

                onUpdate();
            }
            
            modeEntry.OnValueChanged += (_, _) => UpdateBaseEntrySub();
            
            UpdateBaseEntrySub();
        }
        
        internal void RegisterUpdateDelegate(MelonPreferences_Entry<MultiColorMode> modeEntry, Action onUpdate, params MelonPreferences_Entry<string>[] ownColorEntries)
        {
            var prevEntries = new List<MelonPreferences_Entry>();

            void UpdateCurrentEntries()
            {
                prevEntries.Clear();
                
                prevEntries.AddRange(modeEntry.Value switch
                {
                    MultiColorMode.DoNotRecolor => Enumerable.Empty<MelonPreferences_Entry>(),
                    MultiColorMode.UseMainScheme => new []{BaseColorEntry, AccentColorEntry, TextColorEntry, AuxColorEntry},
                    MultiColorMode.UseOwnColors => ownColorEntries,
                    _ => throw new ArgumentOutOfRangeException()
                });
            }
            
            void UpdateBaseEntrySub()
            {
                foreach (var entry in prevEntries) entry.OnValueChangedUntyped -= onUpdate;

                UpdateCurrentEntries();

                foreach (var entry in prevEntries) entry.OnValueChangedUntyped += onUpdate;

                onUpdate();
            }
            
            modeEntry.OnValueChanged += (_, _) => UpdateBaseEntrySub();
            
            UpdateBaseEntrySub();
        }

        internal Color? GetColorForMode(MelonPreferences_Entry<SingleColorMode> modeEntry, MelonPreferences_Entry<string> ownColorEntry)
        {
            return modeEntry.Value switch
            {
                SingleColorMode.DoNotRecolor => null,
                SingleColorMode.UseBaseColor => BaseColor,
                SingleColorMode.UseAccentColor => AccentColor,
                SingleColorMode.UseTextColor => TextColor,
                SingleColorMode.UseAuxColor => AuxColor,
                SingleColorMode.UseOwnColor => ParseColor(ownColorEntry.Value),
                _ => throw new ArgumentOutOfRangeException()
            };
        } 
        
        internal Color? GetColorForMode(MelonPreferences_Entry<MultiColorMode> modeEntry, MelonPreferences_Entry<string> baseColorEntry, MelonPreferences_Entry<string> ownColorEntry)
        {
            return modeEntry.Value switch
            {
                MultiColorMode.DoNotRecolor => null,
                MultiColorMode.UseMainScheme => ParseColor(baseColorEntry.Value),
                MultiColorMode.UseOwnColors => ParseColor(ownColorEntry.Value),
                _ => throw new ArgumentOutOfRangeException()
            };
        } 

        private static void LinkSettingVisibility(MelonPreferences_Entry<SingleColorMode> modeEntry, MelonPreferences_Entry targetEntry)
        {
            var updateDelegate = ExpansionKitApi.RegisterSettingsVisibilityCallback(targetEntry.Category.Identifier,
                targetEntry.Identifier, () => modeEntry.Value == SingleColorMode.UseOwnColor);

            modeEntry.OnValueChangedUntyped += updateDelegate;
        }
        
        private static void LinkSettingVisibility(MelonPreferences_Entry<MultiColorMode> modeEntry, MelonPreferences_Entry targetEntry)
        {
            var updateDelegate = ExpansionKitApi.RegisterSettingsVisibilityCallback(targetEntry.Category.Identifier,
                targetEntry.Identifier, () => modeEntry.Value == MultiColorMode.UseOwnColors);

            modeEntry.OnValueChangedUntyped += updateDelegate;
        }

        public Color BaseColor => ParseColor(BaseColorEntry.Value);
        public Color AccentColor => ParseColor(AccentColorEntry.Value);
        public Color TextColor => ParseColorWithFallback(TextColorEntry.Value, AccentColorEntry.Value);
        public Color AuxColor => ParseColorWithFallback(AuxColorEntry.Value, AccentColorEntry.Value);

        private static Color ParseColorWithFallback(string? mainValue, string fallbackValue)
        {
            return ParseColor(string.IsNullOrEmpty(mainValue?.Trim()) ? fallbackValue : mainValue!);
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

        internal enum SingleColorMode
        {
            [Description("Don't recolor")]
            DoNotRecolor,
            
            [Description("Use base color")]
            UseBaseColor,
            
            [Description("Use accent color")]
            UseAccentColor,
            
            [Description("Use text color")]
            UseTextColor,
            
            [Description("Use auxiliary color")]
            UseAuxColor,
            
            [Description("Use own color")]
            UseOwnColor,
        }
        
        internal enum MultiColorMode
        {
            [Description("Don't recolor")]
            DoNotRecolor,
            
            [Description("Use main color scheme")]
            UseMainScheme,
            
            [Description("Use own colors")]
            UseOwnColors
        }
    }
}