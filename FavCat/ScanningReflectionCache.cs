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

        private static Action<ApiWorld, string, bool>? ourShowWorldInfoPageDelegate;

        public static void DisplayWorldInfoPage(ApiWorld world, string instanceId, bool hasInstanceId)
        {
            if (ourShowWorldInfoPageDelegate == null)
            {
                var target = typeof(UiWorldList)
                    .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static).Single(it =>
                        it.Name.StartsWith("Method_Public_Static_Void_ApiWorld_String_Boolean") &&
                        XrefScanner.XrefScan(it).Any(jt =>
                            jt.Type == XrefType.Global && jt.ReadAsObject()?.ToString() ==
                            "UserInterface/MenuContent/Screens/WorldInfo"));

                ourShowWorldInfoPageDelegate = (Action<ApiWorld, string, bool>) Delegate.CreateDelegate(typeof(Action<ApiWorld, string, bool>), target);
            }

            ourShowWorldInfoPageDelegate(world, instanceId, hasInstanceId);
        }

        public static void DisplayErrorAvatar(this SimpleAvatarPedestal @this)
        {
            if (ourDisplayErrorAvatarDelegate == null)
            {
                var target = typeof(SimpleAvatarPedestal)
                    .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Single(
                        it =>
                        {
                            if (it.ReturnType != typeof(void) || it.Name.Contains("_PDM_")) return false;
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

                            var strings = XrefScanner.XrefScan(it)
                                .Select(jt => jt.Type == XrefType.Global ? jt.ReadAsObject()?.ToString() : null)
                                .Where(jt => jt != null).ToHashSet(); 
                            return strings.Contains("Refreshing with : ");
                        });

                ourPedestalRefreshDelegate =
                    (PedestalRefreshDelegate) Delegate.CreateDelegate(typeof(PedestalRefreshDelegate), target);
            }

            ourPedestalRefreshDelegate(pedestal, avatar);
        }
    }
}