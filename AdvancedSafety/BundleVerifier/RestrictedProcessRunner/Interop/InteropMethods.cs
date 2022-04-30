#nullable enable

using System;
using System.Runtime.InteropServices;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner.Interop
{
    public static class InteropMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);
        
        #region Process
        [DllImport("kernel32.dll", SetLastError=true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcess(
            string? lpApplicationName,
            string? lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [Flags]
        public enum ProcessCreationFlags : uint
        {
            DEBUG_PROCESS = 0x00000001,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            CREATE_SUSPENDED = 0x00000004,
            DETACHED_PROCESS = 0x00000008,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            INHERIT_PARENT_AFFINITY = 0x00010000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            CREATE_SECURE_PROCESS = 0x00400000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NO_WINDOW = 0x08000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO {
            public int  cb;
            public IntPtr  lpReserved;
            public IntPtr  lpDesktop;
            public IntPtr  lpTitle;
            public int  dwX;
            public int  dwY;
            public int  dwXSize;
            public int  dwYSize;
            public int  dwXCountChars;
            public int  dwYCountChars;
            public int  dwFillAttribute;
            public int  dwFlags;
            public short   wShowWindow;
            public short   cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput; // handle
            public IntPtr hStdOutput; // handle
            public IntPtr hStdError; // handle
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION {
            public IntPtr hProcess; // handle
            public IntPtr hThread; // handle
            public int  dwProcessId;
            public int  dwThreadId;
        }
        
        [DllImport("kernel32.dll", SetLastError=true, CharSet = CharSet.Unicode)]
        public static extern bool GetExitCodeProcess(IntPtr  hProcess, out int lpExitCode);
        
        public const int STILL_ACTIVE = 259;
        
        #endregion

        #region Jobs

        [DllImport("kernel32.dll", SetLastError=true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateJobObject(ref SECURITY_ATTRIBUTES lpJobAttributes, string? lpName);
        
        [DllImport("kernel32.dll", SetLastError=true, CharSet = CharSet.Unicode)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoClass jobObjectInformationClass, IntPtr lpJobObjectInformation, int cbJobObjectInformationLength);
        
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool QueryInformationJobObject(IntPtr hJob, JobObjectInfoClass jobObjectInformationClass, IntPtr lpJobObjectInformation, int cbJobObjectInformationLength, out int lpReturnLength);
        
        public enum JobObjectInfoClass
        {
            JobObjectBasicAccountingInformation = 1,
            JobObjectBasicLimitInformation = 2,
            JobObjectBasicProcessIdList = 3,
            JobObjectBasicUIRestrictions = 4,
            JobObjectSecurityLimitInformation = 5,
            JobObjectEndOfJobTimeInformation = 6,
            JobObjectAssociateCompletionPortInformation = 7,
            JobObjectBasicAndIoAccountingInformation = 8,
            JobObjectExtendedLimitInformation = 9,
            JobObjectGroupInformation = 11,
            JobObjectNotificationLimitInformation = 12,
            JobObjectLimitViolationInformation = 13,
            JobObjectGroupInformationEx = 14,
            JobObjectCpuRateControlInformation = 15,
            JobObjectNetRateControlInformation = 32,
            JobObjectNotificationLimitInformation2 = 33,
            JobObjectLimitViolationInformation2 = 34,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_UI_RESTRICTIONS {
            public UiRestrictionClass UIRestrictionsClass;
        }

        [Flags]
        public enum UiRestrictionClass : int
        {
            HANDLES = 0x00000001,
            READCLIPBOARD = 0x00000002,
            WRITECLIPBOARD = 0x00000004,
            SYSTEMPARAMETERS = 0x00000008,
            DISPLAYSETTINGS = 0x00000010,
            GLOBALATOMS = 0x00000020,
            DESKTOP = 0x00000040,
            EXITWINDOWS = 0x00000080,
            
            ALL = 0xff,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JobBasicLimitFlags LimitFlags;
            public IntPtr MinimumWorkingSetSize; // size_t
            public IntPtr MaximumWorkingSetSize; // size_t
            public int ActiveProcessLimit;
            public IntPtr Affinity; // ulong_ptr?
            public int PriorityClass;
            public int SchedulingClass;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS                       IoInfo;
            public IntPtr                            ProcessMemoryLimit; // size_t
            public IntPtr                            JobMemoryLimit; // size_t
            public IntPtr                            PeakProcessMemoryUsed; // size_t
            public IntPtr                            PeakJobMemoryUsed; // size_t
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [Flags]
        public enum JobBasicLimitFlags : int
        {
            WORKINGSET = 0x00000001,
            PROCESS_TIME = 0x00000002,
            JOB_TIME = 0x00000004,
            ACTIVE_PROCESS = 0x00000008,
            AFFINITY = 0x00000010,
            PRIORITY_CLASS = 0x00000020,
            PRESERVE_JOB_TIME = 0x00000040,
            SCHEDULING_CLASS = 0x00000080,
            PROCESS_MEMORY = 0x00000100,
            JOB_MEMORY = 0x00000200,
            DIE_ON_UNHANDLED_EXCEPTION = 0x00000400,
            BREAKAWAY_OK = 0x00000800,
            SILENT_BREAKAWAY_OK = 0x00001000,
            KILL_ON_JOB_CLOSE = 0x00002000,
            SUBSET_AFFINITY = 0x00004000,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_NET_RATE_CONTROL_INFORMATION {
            public long                           MaxBandwidth;
            public JOB_OBJECT_NET_RATE_CONTROL_FLAGS ControlFlags;
            public byte                              DscpTag;
        }
        
        [Flags]
        public enum JOB_OBJECT_NET_RATE_CONTROL_FLAGS {
            JOB_OBJECT_NET_RATE_CONTROL_ENABLE = 0x1,
            JOB_OBJECT_NET_RATE_CONTROL_MAX_BANDWIDTH = 0x2,
            JOB_OBJECT_NET_RATE_CONTROL_DSCP_TAG = 0x4,
        }

        #endregion
    }
}