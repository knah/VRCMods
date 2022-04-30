using System;
using AdvancedSafety.BundleVerifier.RestrictedProcessRunner.Interop;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner
{
    public sealed class RestrictedProcessHandle : IDisposable
    {
        private readonly JobHandle myJobHandle;
        private readonly ProcessHandle myProcessHandle;
        
        public RestrictedProcessHandle(string processPath, string commandline)
        {
            myJobHandle = new JobHandle();

            try
            {
                var cli = $"\"{processPath.Replace("\"", "")}\" {commandline}";
                myProcessHandle = new ProcessHandle(cli, myJobHandle);
            }
            catch
            {
               myJobHandle.Dispose();
               throw;
            }
        }

        public int? WaitForExit(TimeSpan timeout) => myProcessHandle.WaitForExit(timeout);

        public void Dispose()
        {
            myJobHandle.Dispose();
            myProcessHandle.Dispose();
        }

        public void SetLimits(TimeSpan? cpuTimeLimit, ulong? memoryBytes, bool allowNetwork, bool allowDesktop, bool allowChildProcesses) =>
            myJobHandle.SetLimits(cpuTimeLimit, memoryBytes, allowNetwork, allowDesktop, allowChildProcesses);

        public void Start() => myProcessHandle.Start();
    }
}