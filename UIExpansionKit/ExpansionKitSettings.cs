using System.Collections.Generic;
using System.Linq;
using MelonLoader;

namespace UIExpansionKit
{
    public static class ExpansionKitSettings
    {
        private const string KitCategory = "UIExpansionKit";
        private const string PinnedPrefs = "PinnedPrefs";

        internal static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(KitCategory,"UI Expansion Kit");
            
            ModPrefs.RegisterPrefString(KitCategory, PinnedPrefs, "", hideFromList: true);
        }

        public static void PinPref(string category, string prefName)
        {
            SetPinnedPrefs(ListPinnedPrefs(true).Concat(new []{(category, prefName)}).Distinct());
        }
        
        public static void UnpinPref(string category, string prefName)
        {
            SetPinnedPrefs(ListPinnedPrefs(true).Where(it => it != (category, prefName)));
        }
        
        internal static void SetPinnedPrefs(IEnumerable<(string category, string name)> prefs)
        {
            var raw = string.Join(";", prefs.Select(it => $"{it.category},{it.name}"));
            var prefDesc = ModPrefs.GetPrefs()[KitCategory][PinnedPrefs];
            prefDesc.ValueEdited = raw;
        }

        public static IEnumerable<(string category, string name)> ListPinnedPrefs(bool fromTempStore)
        {
            var raw = fromTempStore ? ModPrefs.GetPrefs()[KitCategory][PinnedPrefs].ValueEdited ?? "" : ModPrefs.GetString(KitCategory, PinnedPrefs) ?? "";
            var parts = raw.Split(';');
            return parts.Select(it => it.Split(',')).Where(it => it.Length == 2).Select(it => (it[0], it[1]));
        }
    }
}