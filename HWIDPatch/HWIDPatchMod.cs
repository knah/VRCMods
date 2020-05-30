using System;
using System.Linq;
using System.Reflection;
using HWIDPatch;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

[assembly:MelonModInfo(typeof(HWIDPatchMod), "HWIDPatch", "1.0", "knah")]
[assembly:MelonModGame()]

namespace HWIDPatch
{
    public class HWIDPatchMod : MelonMod
    {
        private static Il2CppSystem.Object ourGeneratedHwidString;
        
        public override unsafe void OnApplicationStart()
        {
            try
            {
                var settingsCategory = "HWIDPatch";
                ModPrefs.RegisterCategory(settingsCategory, "HWID Patch");
                ModPrefs.RegisterPrefString(settingsCategory, "HWID", "", hideFromList: true);

                var newId = ModPrefs.GetString(settingsCategory, "HWID");
                if (newId.Length != SystemInfo.deviceUniqueIdentifier.Length)
                {
                    var random = new System.Random(Environment.TickCount);
                    var bytes = new byte[SystemInfo.deviceUniqueIdentifier.Length / 2];
                    random.NextBytes(bytes);
                    newId = string.Join("", bytes.Select(it => it.ToString("x2")));
                    ModPrefs.SetString(settingsCategory, "HWID", newId);
                }

                ourGeneratedHwidString = new Il2CppSystem.Object(IL2CPP.ManagedStringToIl2Cpp(newId));

                var icallName = "UnityEngine.SystemInfo::GetDeviceUniqueIdentifier";
                var icallAddress = IL2CPP.il2cpp_resolve_icall(icallName);
                if (icallAddress == IntPtr.Zero)
                {
                    MelonModLogger.LogError("Can't resolve the icall, not patching");
                    return;
                }
                
                CompatHook((IntPtr) (&icallAddress),
                    typeof(HWIDPatchMod).GetMethod(nameof(GetDeviceIdPatch),
                        BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                MelonModLogger.Log("Patched HWID; below two should match:");
                MelonModLogger.Log($"Current: {SystemInfo.deviceUniqueIdentifier}");
                MelonModLogger.Log($"Target:  {newId}");
            }
            catch (Exception ex)
            {
                MelonModLogger.LogError(ex.ToString());
            }
        }

        private static IntPtr GetDeviceIdPatch() => ourGeneratedHwidString.Pointer;

        private static void CompatHook(IntPtr first, IntPtr second)
        {
            typeof(Imports).GetMethod("Hook", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                .Invoke(null, new object[] {first, second});
        }
    }
}