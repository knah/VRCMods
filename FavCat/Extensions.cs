using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace FavCat
{
    public static class Extensions
    {
        public static void NoAwait(this Task task)
        {
            task.ContinueWith(tsk =>
            {
                if (tsk.IsFaulted)
                    MelonLogger.LogError($"Free-floating Task failed with exception: {tsk.Exception}");
            });
        }
        
        internal static string StripParenthesis(this string s)
        {
            return Regex.Replace(s, "\\s*\\([0-9\\s]*\\)\\s*", "", RegexOptions.None);
        }

        internal static V GetOrDefault<K, V>(this IDictionary<K, V> dict, K key, V defaultValue)
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }

        internal static void SetSiblingIndex2(this Transform t, int index)
        {
            var currentIndex = t.GetSiblingIndex();
            if(index < currentIndex || index == t.parent.childCount)
                t.SetSiblingIndex(index);
            else if (index == currentIndex)
                return;
            else // this kind of move needs to account for the spot vacated by this transform
                t.SetSiblingIndex(index - 1);
        }

        internal static T GetOrAddComponent<T>(this Component c) where T : Component
        {
            var existing = c.GetComponent<T>();
            if (existing) return existing;
            return c.gameObject.AddComponent<T>();
        }
    }
}