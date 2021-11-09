using System.Collections.Generic;
using MelonLoader;
using UIExpansionKit.API;

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
        
        internal readonly List<(string SettingsValue, string DisplayName)> EnumSettingsInfo = new();
        
        public SettingsHolder()
        {
            var category = MelonPreferences.CreateCategory(CategoryIdentifier, "Styletor");
            
            DisabledMixinsEntry = category.CreateEntry("DisabledMixins", "", is_hidden: true);

            BaseColorEntry = category.CreateEntry("BaseColorString", "0 60 60", "Menu color (red green blue; 0-255)");
            AccentColorEntry = category.CreateEntry("AccentColorString", "106 227 249", "Accent color");
            TextColorEntry = category.CreateEntry("TextColorString", "", "Text color (empty = use accent)");
            
            StyleEntry = category.CreateEntry("SelectedStyle", "default", "Selected style");
            
            ExpansionKitApi.RegisterSettingAsStringEnum(CategoryIdentifier, StyleEntry.Identifier, EnumSettingsInfo);
        }
    }
}