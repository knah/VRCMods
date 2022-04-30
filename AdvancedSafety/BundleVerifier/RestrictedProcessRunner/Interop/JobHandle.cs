using System;
using System.Runtime.InteropServices;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner.Interop
{

    public sealed class JobHandle : IDisposable
    {
        private IntPtr myJobHandle;

        public JobHandle()
        {
            InteropMethods.SECURITY_ATTRIBUTES empty = default;
            myJobHandle = InteropMethods.CreateJobObject(ref empty, null);
            if (myJobHandle == IntPtr.Zero)
            {
                throw new ApplicationException("Unable to create job object");
            }
        }

        internal IntPtr Handle => myJobHandle;

        public unsafe void SetLimits(TimeSpan? cpuTimeLimit, ulong? memoryBytes, bool allowNetwork, bool allowDesktop,
            bool allowChildProcesses)
        {
            {
                InteropMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION limits = default;

                if (cpuTimeLimit != null)
                {
                    limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.PROCESS_TIME;
                    limits.BasicLimitInformation.PerProcessUserTimeLimit = cpuTimeLimit.Value.Ticks;
                }

                if (memoryBytes != null)
                {
                    limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.PROCESS_MEMORY;
                    limits.ProcessMemoryLimit = (IntPtr)memoryBytes.Value;
                }

                if (!allowChildProcesses)
                {
                    limits.BasicLimitInformation.ActiveProcessLimit = 1;
                    limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.ACTIVE_PROCESS;
                }

                limits.BasicLimitInformation.LimitFlags |= InteropMethods.JobBasicLimitFlags.KILL_ON_JOB_CLOSE;

                InteropMethods.SetInformationJobObject(myJobHandle,
                    InteropMethods.JobObjectInfoClass.JobObjectExtendedLimitInformation, (IntPtr)(&limits),
                    Marshal.SizeOf<InteropMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());
            }

            if (!allowDesktop)
            {
                var uiLimits = new InteropMethods.JOBOBJECT_BASIC_UI_RESTRICTIONS
                    { UIRestrictionsClass = InteropMethods.UiRestrictionClass.ALL };
                InteropMethods.SetInformationJobObject(myJobHandle,
                    InteropMethods.JobObjectInfoClass.JobObjectBasicUIRestrictions, (IntPtr)(&uiLimits),
                    Marshal.SizeOf<InteropMethods.JOBOBJECT_BASIC_UI_RESTRICTIONS>());
            }

            if (!allowNetwork)
            {
                var limits = new InteropMethods.JOBOBJECT_NET_RATE_CONTROL_INFORMATION
                {
                    ControlFlags = InteropMethods.JOB_OBJECT_NET_RATE_CONTROL_FLAGS.JOB_OBJECT_NET_RATE_CONTROL_ENABLE |
                                   InteropMethods.JOB_OBJECT_NET_RATE_CONTROL_FLAGS
                                       .JOB_OBJECT_NET_RATE_CONTROL_MAX_BANDWIDTH,
                    MaxBandwidth = 0
                };

                InteropMethods.SetInformationJobObject(myJobHandle,
                    InteropMethods.JobObjectInfoClass.JobObjectNetRateControlInformation, (IntPtr)(&limits),
                    Marshal.SizeOf<InteropMethods.JOBOBJECT_NET_RATE_CONTROL_INFORMATION>());
            }
        }

        private void ReleaseUnmanagedResources()
        {
            if (myJobHandle != IntPtr.Zero) InteropMethods.CloseHandle(myJobHandle);
            myJobHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~JobHandle()
        {
            ReleaseUnmanagedResources();
        }
    }
}