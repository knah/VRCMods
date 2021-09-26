using System;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
using VRC;

namespace AdvancedSafety
{
    public static class ScanningReflectionCache
    {
        private static Action<VRCPlayer, bool> ourReloadAllAvatarsDelegate; 
        
        public static void ReloadAllAvatars(bool excludeSelf)
        {
            // xref yoinked from https://github.com/loukylor/VRC-Mods/commit/11abd2a62caaae5d0f817e20a0f6852d632da6bb#diff-ba8a2891e08655e0e158ea852ef76bbcb1ea58fc831a6817875c5efa21fef610R84 (GPLv3)
            if (ourReloadAllAvatarsDelegate == null)
            {
                var targetMethod = typeof(VRCPlayer).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Single(it => it.Name.StartsWith("Method_Public_Void_Boolean_") && it.GetParameters().Length == 1 &&
                    it.GetParameters()[0].IsOptional && XrefScanner.UsedBy(it).Any(jt =>
                    {
                        if (jt.Type != XrefType.Method) return false;
                        var m = jt.TryResolve();
                        if (m == null) return false;
                        return m.DeclaringType == typeof(FeaturePermissionManager) &&
                               m.Name.StartsWith("Method_Public_Void_");
                    }));
                ourReloadAllAvatarsDelegate = (Action<VRCPlayer, bool>) Delegate.CreateDelegate(typeof(Action<VRCPlayer, bool>), targetMethod);
            }

            ourReloadAllAvatarsDelegate(VRCPlayer.field_Internal_Static_VRCPlayer_0, excludeSelf);
        }
    }
}