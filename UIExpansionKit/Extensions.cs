using UnityEngine;

namespace UIExpansionKit
{
    internal static class Extensions
    {
        public static void DestroyChildren(this Transform parent)
        {
            for (var i = parent.childCount; i > 0; i--) 
                Object.DestroyImmediate(parent.GetChild(i - 1).gameObject);
        }

        public static GameObject NoUnload(this GameObject obj)
        {
            obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return obj;
        }

        public static T GetOrAddComponent<T>(this GameObject obj) where T: Component
        {
            var result = obj.GetComponent<T>();
            if (result == null) 
                result  = obj.AddComponent<T>();
            return result;
        }
    }
}