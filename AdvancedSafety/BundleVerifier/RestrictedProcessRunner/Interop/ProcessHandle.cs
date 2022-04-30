using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner.Interop
{

    public sealed class ProcessHandle : IDisposable
    {
        private IntPtr myProcessHandle;
        private IntPtr myThreadHandle;

        private int myIsStarted;

        public ProcessHandle(string commandLine)
        {
            InteropMethods.SECURITY_ATTRIBUTES empty = default;
            InteropMethods.PROCESS_INFORMATION processInfo = default;
            var startupInfo = new InteropMethods.STARTUPINFO
            {
                cb = Marshal.SizeOf<InteropMethods.STARTUPINFO>(),
            };
            var created = InteropMethods.CreateProcess(null, commandLine, ref empty, ref empty, false,
                InteropMethods.ProcessCreationFlags.CREATE_NO_WINDOW |
                InteropMethods.ProcessCreationFlags.CREATE_SUSPENDED,
                IntPtr.Zero, null, ref startupInfo, out processInfo);
            if (!created)
                throw new ArgumentException("Can't create process");

            myProcessHandle = processInfo.hProcess;
            myThreadHandle = processInfo.hThread;
        }

        public ProcessHandle(string commandLine, JobHandle job) : this(commandLine)
        {
            InteropMethods.AssignProcessToJobObject(job.Handle, myProcessHandle);
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref myIsStarted, 1, 0) == 0)
                InteropMethods.ResumeThread(myThreadHandle);
            else
                throw new ApplicationException("Already started");
        }

        public int? WaitForExit(TimeSpan timeout)
        {
            var waitTime = Stopwatch.StartNew();
            do
            {
                InteropMethods.GetExitCodeProcess(myProcessHandle, out var exitCode);
                if (exitCode != InteropMethods.STILL_ACTIVE)
                    return exitCode;
                Thread.Sleep(33);
            } while (waitTime.Elapsed < timeout);

            return null;
        }

        public int? GetExitCode()
        {
            InteropMethods.GetExitCodeProcess(myProcessHandle, out var exitCode);
            if (exitCode != InteropMethods.STILL_ACTIVE)
                return exitCode;
            return null;
        }

        private void ReleaseUnmanagedResources()
        {
            if (myProcessHandle != IntPtr.Zero) InteropMethods.CloseHandle(myProcessHandle);
            myProcessHandle = IntPtr.Zero;

            if (myThreadHandle != IntPtr.Zero) InteropMethods.CloseHandle(myThreadHandle);
            myThreadHandle = IntPtr.Zero;
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProcessHandle()
        {
            Dispose(false);
        }
    }
}