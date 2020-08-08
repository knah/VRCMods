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
            MelonPrefs.RegisterCategory(CoreLimiterPrefCategory, "Core Limiter");
            MelonPrefs.RegisterInt(CoreLimiterPrefCategory, MaxCoresPref, 4, "Maximum cores");
            MelonPrefs.RegisterBool(CoreLimiterPrefCategory, SkipHyperThreadsPref, true, "Don't use both threads of a core");
            
            MelonLogger.Log($"[CoreLimiter] Have {Environment.ProcessorCount} processor cores");

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

            var targetNumCores = MelonPrefs.GetInt(CoreLimiterPrefCategory, MaxCoresPref);
            var targetBit = processorCount - 1;
            var bitStep = MelonPrefs.GetBool(CoreLimiterPrefCategory, SkipHyperThreadsPref) ? 2 : 1;
            for (var i = 0; i < targetNumCores && targetBit > 0; i++)
            {
                mask |= 1L << targetBit;
                targetBit -= bitStep;
            }
            
            var process = Process.GetCurrentProcess().Handle;
            MelonLogger.Log($"[CoreLimiter] Assigning affinity mask: {mask}");
            SetProcessAffinityMask(process, new IntPtr(mask));
        }
        
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetProcessAffinityMask(IntPtr hProcess, IntPtr dwProcessAffinityMask);
    }
}