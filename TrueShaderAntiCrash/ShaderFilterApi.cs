using System;
using System.Runtime.InteropServices;

namespace TrueShaderAntiCrash
{
    public static class ShaderFilterApi
    {
        public const string DLLName = "DxbcShaderFilter";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void TriBool(bool limitLoops, bool limitGeometry, bool limitTesselation);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OneFloat(float value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void OneInt(int value);

        private static TriBool ourSetFilterState;
        private static OneFloat ourSetTess;
        private static OneInt ourSetLoops;
        private static OneInt ourSetGeom;

        public static void Init(IntPtr hmodule)
        {
            ourSetFilterState = Marshal.GetDelegateForFunctionPointer<TriBool>(GetProcAddress(hmodule, nameof(SetFilteringState)));
            ourSetTess = Marshal.GetDelegateForFunctionPointer<OneFloat>(GetProcAddress(hmodule, nameof(SetMaxTesselationPower)));
            ourSetLoops = Marshal.GetDelegateForFunctionPointer<OneInt>(GetProcAddress(hmodule, nameof(SetLoopLimit)));
            ourSetGeom = Marshal.GetDelegateForFunctionPointer<OneInt>(GetProcAddress(hmodule, nameof(SetGeometryLimit)));
        }
        
        [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        
        public static void SetFilteringState(bool limitLoops, bool limitGeometry, bool limitTesselation) => ourSetFilterState(limitLoops, limitGeometry, limitTesselation);
        public static void SetMaxTesselationPower(float maxTesselation) => ourSetTess(maxTesselation);
        public static void SetLoopLimit(int limit) => ourSetLoops(limit);
        public static void SetGeometryLimit(int limit) => ourSetGeom(limit);
    }
}