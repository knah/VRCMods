using System;
using System.Diagnostics;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner
{
    public sealed class BundleVerifierProcessHandle : IDisposable
    {
        private readonly RestrictedProcessHandle myProcessHandle;

        public BundleVerifierProcessHandle(string executablePath, string sharedMemoryName, TimeSpan maxTime, ulong maxMemory, int minFps, int maxComponents)
        {
            var pid = Process.GetCurrentProcess().Id;

            myProcessHandle = new RestrictedProcessHandle(executablePath, $"-batchmode -nolog -nographics {maxComponents} {pid} {sharedMemoryName} {minFps}");
            
            myProcessHandle.SetLimits(maxTime, maxMemory, false, false, false);
            myProcessHandle.Start();
        }

        public int? WaitForExit(TimeSpan timeout) => myProcessHandle.WaitForExit(timeout);

        public void Dispose()
        {
            myProcessHandle.Dispose();
        }
    }
}