using System;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
using VRC;
using VRC.Core;
using VRC.UI;

namespace FavCat
{
    public static class ScanningReflectionCache
    {
        private delegate void DisplayErrorAvatarDelegate(SimpleAvatarPedestal @this);
        private delegate void PedestalRefreshDelegate(SimpleAvatarPedestal @this, ApiAvatar avatar);
        
        private static DisplayErrorAvatarDelegate? ourDisplayErrorAvatarDelegate;
        private static PedestalRefreshDelegate? ourPedestalRefreshDelegate;

        public static void DisplayErrorAvatar(this SimpleAvatarPedestal @this)
        {
            if (ourDisplayErrorAvatarDelegate == null)
            {
                var target = typeof(SimpleAvatarPedestal)
                    .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Single(
                        it =>
                        {
                            if (it.ReturnType != typeof(void)) return false;
                            var parameters = it.GetParameters();
                            if (parameters.Length != 0)
                                return false;

                            return XrefScanner.XrefScan(it).Any(jt =>
                                jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "local");
                        });

                ourDisplayErrorAvatarDelegate =
                    (DisplayErrorAvatarDelegate) Delegate.CreateDelegate(typeof(DisplayErrorAvatarDelegate), target);
            }

            ourDisplayErrorAvatarDelegate(@this);
        }

        public static void Refresh(this SimpleAvatarPedestal pedestal, ApiAvatar avatar)
        {
            if (ourPedestalRefreshDelegate == null)
            {
                var target = typeof(SimpleAvatarPedestal)
                    .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Single(
                        it =>
                        {
                            if (it.ReturnType != typeof(void)) return false;
                            var parameters = it.GetParameters();
                            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(ApiAvatar))
                                return false;

                            return XrefScanner.XrefScan(it).Any(jt =>
                                jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() == "local");
                        });

                ourPedestalRefreshDelegate =
                    (PedestalRefreshDelegate) Delegate.CreateDelegate(typeof(PedestalRefreshDelegate), target);
            }

            ourPedestalRefreshDelegate(pedestal, avatar);
        }
    }
}