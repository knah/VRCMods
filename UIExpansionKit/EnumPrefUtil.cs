using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace UIExpansionKit
{
    public static class EnumPrefUtil
    {
        public static List<(T SettingsValue, string DisplayName)> GetEnumSettingOptions<T>() where T : Enum
        {
            return typeof(T)
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(it => (it, (T)it.GetValue(null))).Select(it => (it.Item2, GetEnumNameFromField(it.Item1)))
                .ToList();
        }
        
        private static string GetEnumNameFromField(FieldInfo fi) => fi.GetCustomAttribute<DescriptionAttribute>()?.Description ?? fi.Name;
    }
}