using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using MelonLoader;

namespace AdvancedSafety
{
    public class ReaderPatches
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr AudioMixerReadDelegate(IntPtr thisPtr, IntPtr readerPtr);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void FloatReadDelegate(IntPtr readerPtr, float* result, byte* fieldName);

        private static AudioMixerReadDelegate ourAudioMixerReadDelegate;
        private static IntPtr ourAudioMixerReadPointer;

        private static FloatReadDelegate ourFloatReadDelegate;
        private static IntPtr ourFloatReadPointer;
        
        // Why these numbers? Check wrld_b9f80349-74af-4840-8ce9-a1b783436590 for how *horribly* things break even on 10^6. Nothing belongs outside these bounds. The significand is that of MaxValue.
        private const float MaxAllowedValueTop = 3.402823E+7f;
        private const float MaxAllowedValueBottom = -3.402823E+7f;
        
        private static readonly List<object> ourPinnedDelegates = new ();

        private static readonly string[] ourAllowedFields = { "m_BreakForce", "m_BreakTorque", "collisionSphereDistance", "maxDistance", "inSlope", "outSlope" };

        private static readonly Dictionary<string, (int ProduceMixer, int TransferFloat)> ourOffsets = new() {
            { "aCEmIwSIcjYriBQDFjQlpTNNW1/kA8Wlbkqelmt1USOMB09cnKwK7QWyOulz9d7DEYJh4+vO0Ldv8gdH+dZCrg==", (0x4997C0, 0xD7320) }, // U2018.4.20 non-dev
            { "5dkhl/dWeTREXhHCIkZK17mzZkbjhTKlxb+IUSk+YaWzZrrV+G+M0ekTOEGjZ4dJuB4O3nU/oE3dycXWeJq9uA==", (0xA80660, 0xC7B40) }, // U2019.4.28 non-dev
            { "MV6xP7theydao4ENbGi6BbiBxdZsgGOBo/WrPSeIqh6A/E00NImjUNZn+gL+ZxzpVbJms7nUb6zluLL3+aIcfg==", (0xA82350, 0xC7E50) }, // U2019.4.29 non-dev
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
                    ourAudioMixerReadPointer = module.BaseAddress + offsets.ProduceMixer;
                    var patchDelegate = new AudioMixerReadDelegate(AudioMixerReadPatch);
                    ourPinnedDelegates.Add(patchDelegate);
                    fixed (IntPtr* mixerReadAddress = &ourAudioMixerReadPointer) 
                        MelonUtils.NativeHookAttach((IntPtr)mixerReadAddress, Marshal.GetFunctionPointerForDelegate(patchDelegate));
                    ourAudioMixerReadDelegate = Marshal.GetDelegateForFunctionPointer<AudioMixerReadDelegate>(ourAudioMixerReadPointer);
                }

                unsafe {
                    // SafeBinaryRead::Transfer<float>
                    ourFloatReadPointer = module.BaseAddress + offsets.TransferFloat;
                    var patchDelegate = new FloatReadDelegate(FloatTransferPatch);
                    ourPinnedDelegates.Add(patchDelegate);
                    fixed (IntPtr* floatReadAddress = &ourFloatReadPointer) 
                        MelonUtils.NativeHookAttach((IntPtr)floatReadAddress, Marshal.GetFunctionPointerForDelegate(patchDelegate));
                    ourFloatReadDelegate = Marshal.GetDelegateForFunctionPointer<FloatReadDelegate>(ourFloatReadPointer);
                }

                break;
            }
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
            ourAudioMixerReadDelegate ??= Marshal.GetDelegateForFunctionPointer<AudioMixerReadDelegate>(ourAudioMixerReadPointer);
            return ourAudioMixerReadDelegate(thisPtr, readerPtr);
        }
    }
}