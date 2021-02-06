using System.Collections.Generic;
using System.Linq;
using MelonLoader;

namespace UIExpansionKit
{
    public static class ExpansionKitSettings
    {
        private const string KitCategory = "UIExpansionKit";
        private const string PinnedPrefs = "PinnedPrefs";
        private const string QmExpandoStartsCollapsed = "QmExpandoStartsCollapsed";
        private const string CategoriesStartCollapsed = "CategoriesStartCollapsed";
        private const string QmExpandoMinRows = "QmExpandoMinRows";
        private const string QmExpandoMaxRows = "QmExpandoMaxRows";

        internal static MelonPreferences_Entry<string> PinsEntry;

        internal static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory(KitCategory,"UI Expansion Kit");
            
            PinsEntry = (MelonPreferences_Entry<string>) category.CreateEntry(PinnedPrefs, "", is_hidden: true);
            
            category.CreateEntry(QmExpandoStartsCollapsed, false, "Quick Menu extra panel starts hidden");
            category.CreateEntry(CategoriesStartCollapsed, false, "Settings categories start collapsed");
            
            category.CreateEntry(QmExpandoMinRows, 1, "Minimum rows in Quick Menu extra panel");
            category.CreateEntry(QmExpandoMaxRows, 3, "Maximum rows in Quick Menu extra panel");
        }

        public static bool IsQmExpandoStartsCollapsed() => MelonPreferences.GetEntryValue<bool>(KitCategory, QmExpandoStartsCollapsed);
        public static bool IsCategoriesStartCollapsed() => MelonPreferences.GetEntryValue<bool>(KitCategory, CategoriesStartCollapsed);

        public static int ClampQuickMenuExpandoRowCount(int targetCount)
        {
            var min = MelonPreferences.GetEntryValue<int>(KitCategory, QmExpandoMinRows);
            var max = MelonPreferences.GetEntryValue<int>(KitCategory, QmExpandoMaxRows);

            if (targetCount < min) return min;
            if (targetCount > max) return max;
            return targetCount;
        }

        public static void PinPref(string category, string prefName)
        {
            SetPinnedPrefs(ListPinnedPrefs().Concat(new []{(category, prefName)}).Distinct());
        }
        
        public static void UnpinPref(string category, string prefName)
        {
            SetPinnedPrefs(ListPinnedPrefs().Where(it => it != (category, prefName)));
        }

        public static bool IsPinned(string category, string prefName)
        {
            return ListPinnedPrefs().Contains((category, prefName));
        }
        
        internal static void SetPinnedPrefs(IEnumerable<(string category, string name)> prefs)
        {
            var raw = string.Join(";", prefs.Select(it => $"{it.category},{it.name}"));
            if (PinsEntry.Value != raw)
                PinsEntry.Value = raw;
        }

        public static IEnumerable<(string category, string name)> ListPinnedPrefs()
        {
            var raw = PinsEntry.Value;
            var parts = raw.Split(';');
            return parts.Select(it => it.Split(',')).Where(it => it.Length == 2).Select(it => (it[0], it[1]));
        }
    }
}