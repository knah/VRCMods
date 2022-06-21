using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MelonLoader;
using System.Reflection;
using HarmonyLib;
using Turbones;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnhollowerRuntimeLib.XrefScans;

[assembly:MelonInfo(typeof(TurbonesMod), "Turbones", "1.1.2", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace Turbones
{
    internal partial class TurbonesMod : MelonMod
    {
        public static MelonLogger.Instance Logger;
        
        private static IntPtr ourDynBoneCollideEntryPoint;
        private static IntPtr ourDynBoneUpdateEntryPoint;
        private static IntPtr ourLastPatchPointer;
        
        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<BoneDeleteHandler>();

            Logger = LoggerInstance;
            
            var category = MelonPreferences.CreateCategory("Turbones");
            var enableCollisionChecks = category.CreateEntry("OptimizedCollisionChecks", true, "Enable optimized collision checks");
            var enableUpdate = category.CreateEntry("OptimizedUpdate", true, "Enable optimized simulation");
            var updateMultiThread = category.CreateEntry("OptimizedMultiThread", false, "Enable multithreading (placebo!)");
            var threadCount = category.CreateEntry("DynamicBoneThreads", Math.Max(1, Environment.ProcessorCount / 2 - 1), "Thread count (placebo!)", dont_save_default: true);
            
            var dllName = "JigglyRustSolver.dll";

            try
            {
                using var resourceStream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(typeof(TurbonesMod), dllName);
                using var fileStream = File.Open("VRChat_Data/Plugins/" + dllName, FileMode.Create, FileAccess.Write);

                resourceStream.CopyTo(fileStream);
            }
            catch (IOException ex)
            {
                Logger.Warning("Failed to write native dll; will attempt loading it anyway. This is normal if you're running multiple instances of VRChat");
                MelonDebug.Msg(ex.ToString());
            }

            if (!JigglySolverApi.Initialize("VRChat_Data/Plugins/" + dllName))
            {
                Logger.Error("Error initializing native library; mod won't work");
                return;
            }

            ourDynBoneCollideEntryPoint = Marshal.ReadIntPtr((IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(
                typeof(DynamicBoneCollider).GetMethod(nameof(DynamicBoneCollider
                    .Method_Public_Void_byref_Vector3_Single_0))).GetValue(null));
            
            ourDynBoneUpdateEntryPoint = Marshal.ReadIntPtr((IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(
                typeof(DynamicBone).GetMethod(nameof(DynamicBone
                    .Method_Private_Void_Single_Boolean_0))).GetValue(null));
            
            var isCollidePatched = false;
            

            unsafe void PatchCollide()
            {
                if (isCollidePatched) return;
                
                fixed(IntPtr* a = &ourDynBoneCollideEntryPoint)
                    MelonUtils.NativeHookAttach((IntPtr)a, JigglySolverApi.LibDynBoneCollideEntryPoint);

                Logger.Msg("Patched DynamicBone Collide");
                isCollidePatched = true;
            }

            unsafe void UnpatchCollide()
            {
                if (!isCollidePatched) return;
                
                fixed(IntPtr* a = &ourDynBoneCollideEntryPoint)
                    MelonUtils.NativeHookDetach((IntPtr)a, JigglySolverApi.LibDynBoneCollideEntryPoint);
                
                Logger.Msg("Unpatched DynamicBone Collide");
                isCollidePatched = false;
            }

            unsafe void RepatchUpdate(bool useFast, bool useMt)
            {
                // TODO: re-enable multithreading if it ever gets useful/stable
                useMt = false;
                
                if (ourLastPatchPointer != IntPtr.Zero)
                {
                    fixed(IntPtr* a = &ourDynBoneUpdateEntryPoint)
                        MelonUtils.NativeHookDetach((IntPtr)a, ourLastPatchPointer);
                    
                    Logger.Msg("Unpatched DynamicBone Update");
                    ourLastPatchPointer = IntPtr.Zero;
                }
                
                if (!CheckWasSuccessful) return;
                
                if (useFast)
                {
                    ourLastPatchPointer = useMt ? JigglySolverApi.LibDynBoneUpdateMultiThreaded : JigglySolverApi.LibDynBoneUpdateSingleThreaded;
                    
                    fixed(IntPtr* a = &ourDynBoneUpdateEntryPoint)
                        MelonUtils.NativeHookAttach((IntPtr)a, ourLastPatchPointer);

                    Logger.Msg($"Patched DynamicBone Update (multithreaded: {useMt})");
                }
                else
                {
                    ourLastPatchPointer = JigglySolverApi.DynamicBoneUpdateNotifyPatch;
                    
                    fixed(IntPtr* a = &ourDynBoneUpdateEntryPoint)
                        MelonUtils.NativeHookAttach((IntPtr)a, ourLastPatchPointer);

                    JigglySolverApi.SetOriginalBoneUpdateDelegate(ourDynBoneUpdateEntryPoint);

                    Logger.Msg($"Patched DynamicBone Update (notify)");
                }
            }
            
            CheckDummyThree();

            enableCollisionChecks.OnValueChanged += (_, v) =>
            {
                if (v) PatchCollide();
                else UnpatchCollide();
            };

            if (enableCollisionChecks.Value) PatchCollide();

            enableUpdate.OnValueChanged += (_, v) => RepatchUpdate(v, updateMultiThread.Value);
            updateMultiThread.OnValueChanged += (_, v) => RepatchUpdate(enableUpdate.Value, v);
            
            RepatchUpdate(enableUpdate.Value, updateMultiThread.Value);

            threadCount.OnValueChanged += (_, v) => JigglySolverApi.SetNumThreads(Math.Max(Math.Min(v, 32), 1));
            JigglySolverApi.SetNumThreads(Math.Max(Math.Min(threadCount.Value, 32), 1));

            HarmonyInstance.Patch(typeof(DynamicBone).GetMethod(nameof(DynamicBone.OnEnable)), new HarmonyMethod(typeof(TurbonesMod), nameof(OnEnablePrefix)));
            HarmonyInstance.Patch(typeof(DynamicBone).GetMethod(nameof(DynamicBone.OnDisable)), new HarmonyMethod(typeof(TurbonesMod), nameof(OnDisablePrefix)));
            HarmonyInstance.Patch(typeof(AvatarClone).GetMethod(nameof(AvatarClone.LateUpdate)), new HarmonyMethod(typeof(TurbonesMod), nameof(LateUpdatePrefix)));
            HarmonyInstance.Patch(XrefScanner.XrefScan(typeof(DynamicBone).GetMethod(nameof(DynamicBone.OnEnable)))
                    .Single(it => it.Type == XrefType.Method && it.TryResolve() != null).TryResolve(),
                new HarmonyMethod(typeof(TurbonesMod), nameof(ResetParticlesPatch)));
        }

        public static void ResetParticlesPatch(DynamicBone __instance)
        {
            JigglySolverApi.ResetParticlePositions(__instance.Pointer);
        }

        public override void OnUpdate()
        {
            JigglySolverApi.FlushColliderCache();
        }

        private static void LateUpdatePrefix()
        {
            JigglySolverApi.JoinMultithreadedJobs();
        }

        public static void OnEnablePrefix(DynamicBone __instance)
        {
            JigglySolverApi.DynamicBoneOnEnablePatch(__instance.Pointer);

            if (__instance.gameObject.GetComponent<BoneDeleteHandler>() == null)
                __instance.gameObject.AddComponent<BoneDeleteHandler>();
        }

        public static void OnDisablePrefix(DynamicBone __instance)
        {
            JigglySolverApi.DynamicBoneOnDisablePatch(__instance.Pointer);
        }

        public static void OnStartSuffix(DynamicBone __instance)
        {
            JigglySolverApi.DynamicBoneStartPatch(__instance.Pointer);
        }

        public static void OnDestroyInjected(DynamicBone __instance)
        {
            JigglySolverApi.DynamicBoneOnDestroyPatch(__instance.Pointer);
        }
    }
}