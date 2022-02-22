using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using MelonLoader;

namespace AdvancedSafety
{
    public class ReaderPatches
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr AudioMixerReadDelegate(IntPtr thisPtr, IntPtr readerPtr);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void FloatReadDelegate(IntPtr readerPtr, float* result, byte* fieldName);

        private static volatile AudioMixerReadDelegate ourAudioMixerReadDelegate;

        private static FloatReadDelegate ourFloatReadDelegate;
        
        // Why these numbers? Check wrld_b9f80349-74af-4840-8ce9-a1b783436590 for how *horribly* things break even on 10^6. Nothing belongs outside these bounds. The significand is that of MaxValue.
        private const float MaxAllowedValueTop = 3.402823E+7f;
        private const float MaxAllowedValueBottom = -3.402823E+7f;
        
        private static readonly List<object> ourPinnedDelegates = new ();

        private static readonly string[] ourAllowedFields = { "m_BreakForce", "m_BreakTorque", "collisionSphereDistance", "maxDistance", "inSlope", "outSlope" };

        private static readonly Dictionary<string, (int ProduceMixer, int TransferFloat, int CountNodes, int DebugAssert, int ReaderOOB, int ReallocateString)> ourOffsets = new() {
            { "aCEmIwSIcjYriBQDFjQlpTNNW1/kA8Wlbkqelmt1USOMB09cnKwK7QWyOulz9d7DEYJh4+vO0Ldv8gdH+dZCrg==", (0x4997C0, 0xD7320, 0, 0, 0, 0) }, // U2018.4.20 non-dev
            { "5dkhl/dWeTREXhHCIkZK17mzZkbjhTKlxb+IUSk+YaWzZrrV+G+M0ekTOEGjZ4dJuB4O3nU/oE3dycXWeJq9uA==", (0xA80660, 0xC7B40, 0, 0, 0, 0) }, // U2019.4.28 non-dev
            { "MV6xP7theydao4ENbGi6BbiBxdZsgGOBo/WrPSeIqh6A/E00NImjUNZn+gL+ZxzpVbJms7nUb6zluLL3+aIcfg==", (0xA82350, 0xC7E50, 0, 0, 0, 0) }, // U2019.4.29 non-dev
            { "ccZ4F7iE7a78kWdXdMekJzP7/ktzS5jOOS8IOITxa1C5Jg2TKxC0/ywY8F0o9I1vZHsxAO4eh7G2sOGzsR/+uQ==", (0xA83410, 0xC7E80, 0, 0, 0, 0) }, // U2019.4.30 non-dev
            { "sgZUlX3+LSHKnTiTC+nXNcdtLOTrAB1fNjBLOwDdKzCyndlFLAdL0udR4S1szTC/q5pnFhG3Kdspsj5jvwLY1A==", (0xA86270, 0xC8230, 0xDF29F0, 0xDDBDC0, 0x7B9EB0, 0xC69F0) }, // U2019.4.31 non-dev
        };

        internal static void ApplyPatches()
        {
            string unityPlayerHash;
            {
                using var sha = SHA512.Create();
                using var unityPlayerStream = File.OpenRead("UnityPlayer.dll");
                unityPlayerHash = Convert.ToBase64String(sha.ComputeHash(unityPlayerStream));
            }

            if (!ourOffsets.TryGetValue(unityPlayerHash, out var offsets))
            {
                MelonLogger.Error($"Unknown UnityPlayer hash: {unityPlayerHash}");
                MelonLogger.Error("Some features will not work");
                return;
            }

            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (!module.FileName.Contains("UnityPlayer")) continue;

                unsafe {
                    // ProduceHelper<AudioMixer,0>::Produce, thanks to Ben for finding an adjacent method
                    DoPatch(module, offsets.ProduceMixer, AudioMixerReadPatch, out ourAudioMixerReadDelegate);

                    // SafeBinaryRead::Transfer<float>
                    DoPatch(module, offsets.TransferFloat, FloatTransferPatch, out ourFloatReadDelegate);

                    // CountNodesDeep, thanks to Requi and Ben for this and idea for next two
                    DoPatch<CountNodesDeepDelegate>(module, offsets.CountNodes, CountNodesDeepThunk, out _);
                    
                    // DebugStringToFilePostprocessedStacktrace
                    DoPatch(module, offsets.DebugAssert, DebugAssertPatch, out ourOriginalAssert);

                    // CachedReader::OutOfBoundsError
                    DoPatch(module, offsets.ReaderOOB, ReaderOobPatch, out ourOriginalReaderOob);
                    
                    // core::StringStorageDefault<char>::reallocate, identified to be an issue by Requi&Ben
                    DoPatch(module, offsets.ReallocateString, ReallocateStringPatch, out ourOriginalRealloc);
                }

                break;
            }
        }

        private static unsafe void DoPatch<T>(ProcessModule module, int offset, T patchDelegate, out T delegateField) where T : MulticastDelegate
        {
            delegateField = null;
            if (offset == 0) return;
            var targetPtr = module.BaseAddress + offset;
            ourPinnedDelegates.Add(patchDelegate);
            MelonUtils.NativeHookAttach((IntPtr)(&targetPtr), Marshal.GetFunctionPointerForDelegate(patchDelegate));
            delegateField = Marshal.GetDelegateForFunctionPointer<T>(targetPtr);
        }

        private static unsafe void FloatTransferPatch(IntPtr reader, float* result, byte* fieldName)
        {
            ourFloatReadDelegate(reader, result, fieldName);

            if (AdvancedSafetyMod.CanReadBadFloats || *result > MaxAllowedValueBottom && *result < MaxAllowedValueTop || AdvancedSafetySettings.AllowReadingBadFloats.Value) return;

            if (float.IsNaN(*result)) goto clamp;
            
            if (fieldName != null)
            {
                foreach (var allowedField in ourAllowedFields)
                {
                    for (var j = 0; j < allowedField.Length; j++)
                        if (fieldName[j] == 0 || fieldName[j] != allowedField[j])
                            goto next;
                    return;
                    next: ;
                }
            }
            
            clamp:

            if (MelonDebug.IsEnabled())
                MelonDebug.Msg($"Clamped a float to 0: {*result} {Marshal.PtrToStringAnsi((IntPtr)fieldName)}");

            *result = 0;
        }

        private static IntPtr AudioMixerReadPatch(IntPtr thisPtr, IntPtr readerPtr)
        {
            if (!AdvancedSafetyMod.CanReadAudioMixers && !AdvancedSafetySettings.AllowReadingMixers.Value)
            {
                MelonDebug.Msg("Not reading audio mixer");
                return IntPtr.Zero;
            }

            // just in case something ever races
            while (ourAudioMixerReadDelegate == null) Thread.Sleep(15);
            return ourAudioMixerReadDelegate(thisPtr, readerPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ReaderOobDelegate(IntPtr thisPtr, long a, long b);

        private static ReaderOobDelegate ourOriginalReaderOob;
        [ThreadStatic]
        private static int ourReaderOobDepth;

        private static void ReaderOobPatch(IntPtr thisPtr, long a, long b)
        {
            ourReaderOobDepth++;
            try
            {
                ourOriginalReaderOob(thisPtr, a, b);
            }
            finally
            {
                ourReaderOobDepth--;
            }
        }
        

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DebugAssertDelegate(IntPtr data);

        private static DebugAssertDelegate ourOriginalAssert;
        private static unsafe void DebugAssertPatch(IntPtr data)
        {
            if (AdvancedSafetySettings.RiskyAssertDisable.Value && ourReaderOobDepth > 0) 
                *(byte*)(data + 0x30) &= 0xef;

            ourOriginalAssert(data);
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate long CountNodesDeepDelegate(NodeContainer* thisPtr);

        private static unsafe long CountNodesDeepThunk(NodeContainer* thisPtr)
        {
            try
            {
                return CountNodesDeepImpl(thisPtr, new HashSet<IntPtr>());
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception in CountNodes patch: {ex}");
                return 1;
            }
        }

        private static unsafe long CountNodesDeepImpl(NodeContainer* thisPtr, HashSet<IntPtr> parents)
        {
            if (thisPtr == null) return 1;

            var directSubsCount = thisPtr->DirectSubCount;
            
            long totalNodes = 1;
            if (directSubsCount <= 0)
                return totalNodes;

            parents.Add((IntPtr) thisPtr);

            var subsBase = thisPtr->Subs;
            if (subsBase == null)
            {
                // Unlikely, but better be safe
                thisPtr->DirectSubCount = 0;
                return totalNodes;
            }

            for (var i = 0; i < directSubsCount; ++i)
            {
                var subNode = subsBase[i];

                if (subNode == null)
                {
                    MelonDebug.Msg("Owww. My other right toe hurts!");
                    thisPtr->DirectSubCount = 0;
                    return totalNodes;
                }

                if (parents.Contains((IntPtr) subNode))
                {
                    MelonDebug.Msg("Owww. My right toe hurts!");
                    subNode->DirectSubCount = thisPtr->DirectSubCount = 0;
                    return totalNodes;
                }

                totalNodes += CountNodesDeepImpl(subNode, parents);
            }

            return totalNodes;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x88)]
        private unsafe struct NodeContainer
        {
            [FieldOffset(0x70)]
            public NodeContainer** Subs;
            [FieldOffset(0x80)]
            public long DirectSubCount;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate IntPtr StringReallocateDelegate(NativeString* str, long newSize);

        private static StringReallocateDelegate ourOriginalRealloc;

        [ThreadStatic] private static unsafe NativeString* ourLastReallocatedString;
        [ThreadStatic] private static int ourLastReallocationCount;

        private static unsafe IntPtr ReallocateStringPatch(NativeString* str, long newSize)
        {
            if (str != null && newSize > 128 && str->Data != IntPtr.Zero)
            {
                if (ourLastReallocatedString != str)
                {
                    ourLastReallocatedString = str;
                    ourLastReallocationCount = 0;
                }
                else
                {
                    ourLastReallocationCount++;
                    if (ourLastReallocationCount >= 8 && newSize <= str->Capacity + 16 && str->Capacity > 16)
                        newSize = str->Capacity * 2;
                }
            }

            while (ourOriginalRealloc == null) Thread.Sleep(15);
            return ourOriginalRealloc(str, newSize);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeString
        {
            public IntPtr Data;
            public long Capacity;
            public long Unknown;
            public long Length;
        }
    }
}