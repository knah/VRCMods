using System;
using System.Runtime.InteropServices;
using MelonLoader;
using UnhollowerBaseLib;

namespace Turbones
{
    public static class JigglySolverApi
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetPointersAndOffsetsDelegate(
            ref ICallOffsets icalls,
            ref ColliderComponentOffsets component_offsets,
            ref BoneComponentOffsets bone_offsets,
            ref ParticleClassOffsets particle_offsets,
            ref ListClassOffsets list_offsets,
            ref ObjectOffsets object_offsets
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void VoidDelegate();
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetThreadNumberDelegate(int threadNumber);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ComponentDelegate(IntPtr boneComponent);


        internal static VoidDelegate FlushColliderCache { get; private set; } 
        internal static VoidDelegate JoinMultithreadedJobs { get; private set; } 
        internal static SetPointersAndOffsetsDelegate SetPointersAndOffsets { get; private set; }
        internal static SetThreadNumberDelegate SetNumThreads { get; private set; }
        
        internal static ComponentDelegate DynamicBoneOnEnablePatch { get; private set; }
        internal static ComponentDelegate DynamicBoneOnDisablePatch { get; private set; }
        internal static ComponentDelegate DynamicBoneStartPatch { get; private set; }
        internal static ComponentDelegate DynamicBoneOnDestroyPatch { get; private set; }

        public delegate void RegisterColliderForCollisionFeedbackDelegate(IntPtr colliderPtr, byte group);
        public delegate void UnregisterColliderForCollisionFeedbackDelegate(IntPtr colliderPtr);
        public delegate ulong GetAndClearCollidingGroupsMaskDelegate();

        /// <summary>
        /// Registers the specified collider for collision feedback with the specified group.
        /// Valid value for group is between 0 and 63.
        /// If the collider is destroyed, you MUST unregister it.
        /// </summary>
        public static RegisterColliderForCollisionFeedbackDelegate RegisterColliderForCollisionFeedback { get; private set; }
        
        /// <summary>
        /// Removes the specified collider for collision feedback.
        /// </summary>
        public static UnregisterColliderForCollisionFeedbackDelegate UnregisterColliderForCollisionFeedback { get; private set; }
        
        /// <summary>
        /// Removes all currently registered colliders from collision feedback
        /// </summary>
        public static VoidDelegate ClearCollisionFeedbackColliders { get; private set; }
        
        /// <summary>
        /// Gets the collision feedback mask for colliders that have collided since the last get.
        /// The mask has bits set corresponding to groups of registered colliders.
        /// For example, if a collider was registered in group 2, and it and only it has collided, the mask will be set to 0100b = 4.
        /// </summary>
        public static GetAndClearCollidingGroupsMaskDelegate GetAndClearCollidingGroupsMask { get; private set; }
        
        
        internal static IntPtr LibDynBoneCollideEntryPoint;
        internal static IntPtr LibDynBoneUpdateSingleThreaded;
        internal static IntPtr LibDynBoneUpdateMultiThreaded;

        internal static bool Initialize(string dllName)
        {
            var lib = LoadLibraryA(dllName);
            if (lib == IntPtr.Zero)
            {
                MelonLogger.Error("Native library load failed, mod won't work");
                return false;
            }

            SetPointersAndOffsets = Marshal.GetDelegateForFunctionPointer<SetPointersAndOffsetsDelegate>(GetProcAddress(lib, nameof(SetPointersAndOffsets)));
            FlushColliderCache = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(GetProcAddress(lib, nameof(FlushColliderCache)));
            JoinMultithreadedJobs = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(GetProcAddress(lib, nameof(JoinMultithreadedJobs)));
            SetNumThreads = Marshal.GetDelegateForFunctionPointer<SetThreadNumberDelegate>(GetProcAddress(lib, nameof(SetNumThreads)));
            
            DynamicBoneOnDestroyPatch = Marshal.GetDelegateForFunctionPointer<ComponentDelegate>(GetProcAddress(lib, nameof(DynamicBoneOnDestroyPatch)));
            DynamicBoneOnEnablePatch = Marshal.GetDelegateForFunctionPointer<ComponentDelegate>(GetProcAddress(lib, nameof(DynamicBoneOnEnablePatch)));
            DynamicBoneOnDisablePatch = Marshal.GetDelegateForFunctionPointer<ComponentDelegate>(GetProcAddress(lib, nameof(DynamicBoneOnDisablePatch)));
            DynamicBoneStartPatch = Marshal.GetDelegateForFunctionPointer<ComponentDelegate>(GetProcAddress(lib, nameof(DynamicBoneStartPatch)));
            
            RegisterColliderForCollisionFeedback = Marshal.GetDelegateForFunctionPointer<RegisterColliderForCollisionFeedbackDelegate>(GetProcAddress(lib, nameof(RegisterColliderForCollisionFeedback)));
            UnregisterColliderForCollisionFeedback = Marshal.GetDelegateForFunctionPointer<UnregisterColliderForCollisionFeedbackDelegate>(GetProcAddress(lib, nameof(UnregisterColliderForCollisionFeedback)));
            ClearCollisionFeedbackColliders = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(GetProcAddress(lib, nameof(ClearCollisionFeedbackColliders)));
            GetAndClearCollidingGroupsMask = Marshal.GetDelegateForFunctionPointer<GetAndClearCollidingGroupsMaskDelegate>(GetProcAddress(lib, nameof(GetAndClearCollidingGroupsMask)));

            LibDynBoneCollideEntryPoint = GetProcAddress(lib, "ColliderCollidePatch");
            LibDynBoneUpdateSingleThreaded = GetProcAddress(lib, "DynamicBoneUpdateSingleThreadPatch");
            LibDynBoneUpdateMultiThreaded = GetProcAddress(lib, "DynamicBoneUpdateMultiThreadPatch");
            
            Offsets.SetOffsets();
            
            return true;
        }
        
        
        [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        
        [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        internal static extern IntPtr LoadLibraryA(string libName);
    }
}