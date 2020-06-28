using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CoreLimiter;
using MelonLoader;

[assembly:MelonModInfo(typeof(CoreLimiterMod), "Core Limiter", "1.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonModGame]

namespace CoreLimiter
{
    public class CoreLimiterMod : MelonMod
    {
        private const string CoreLimiterPrefCategory = "CoreLimiter";
        private const string MaxCoresPref = "MaxCores";
        private const string SkipHyperThreadsPref = "SkipHyperThreads";

        public override void OnApplicationStart()
        {
            ModPrefs.RegisterCategory(CoreLimiterPrefCategory, "Core Limiter");
            ModPrefs.RegisterPrefInt(CoreLimiterPrefCategory, MaxCoresPref, 4, "Maximum cores");
            ModPrefs.RegisterPrefBool(CoreLimiterPrefCategory, SkipHyperThreadsPref, true, "Don't use both threads of a core");
            
            MelonModLogger.Log($"[CoreLimiter] Have {Environment.ProcessorCount} processor cores");

            ApplyAffinity();
        }

        public override void OnModSettingsApplied()
        {
            ApplyAffinity();
        }

        private static void ApplyAffinity()
        {
            var processorCount = Environment.ProcessorCount;
            long mask = 0;

            var targetNumCores = ModPrefs.GetInt(CoreLimiterPrefCategory, MaxCoresPref);
            var targetBit = processorCount - 1;
            var bitStep = ModPrefs.GetBool(CoreLimiterPrefCategory, SkipHyperThreadsPref) ? 2 : 1;
            for (var i = 0; i < targetNumCores && targetBit > 0; i++)
            {
                mask |= 1L << targetBit;
                targetBit -= bitStep;
            }
            
            var process = Process.GetCurrentProcess().Handle;
            MelonModLogger.Log($"[CoreLimiter] Assigning affinity mask: {mask}");
            SetProcessAffinityMask(process, new IntPtr(mask));
        }
        
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetProcessAffinityMask(IntPtr hProcess, IntPtr dwProcessAffinityMask);
    }
}