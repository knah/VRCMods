using System;
using System.Runtime.InteropServices;

namespace AdvancedSafety
{
    // These are copypasted from Unhollower. TODO: use actual structs from unhollower once/if they become public
    [StructLayout(LayoutKind.Sequential)]
    internal struct Il2CppFieldInfo_24_1
    {
        public IntPtr name; // const char*
        public IntPtr typePtr; // const
        public IntPtr parentClassPtr; // non-const?
        public int offset; // If offset is -1, then it's thread static
        public uint token;
    }
}