using System;
using System.Runtime.InteropServices;
using Finitizer;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(FinitizerMod), "Finitizer", "1.3.2", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace Finitizer
{
    internal partial class FinitizerMod : MelonMod
    {
        private const string SettingsCategory = "Finitizer";
        private const string EnabledSetting = "Enabled";
        
        // Why these numbers? Check wrld_b9f80349-74af-4840-8ce9-a1b783436590 for how *horribly* things break even on 10^6. Nothing belongs outside these bounds. The significand is that of MaxValue.
        private const float MaxAllowedValueTop = 3.402823E+7f;
        private const float MaxAllowedValueBottom = -3.402823E+7f;
        
        private static MelonLogger.Instance Logger;

        private bool myArePatchesApplied;
        private bool myWasEnabled;
        
        public override void OnApplicationStart()
        {
            Logger = LoggerInstance;
            
            var category = MelonPreferences.CreateCategory(SettingsCategory, SettingsCategory);
            var entry = category.CreateEntry(EnabledSetting, true, "FP fix enabled");
            entry.OnValueChanged += (_, value) =>
            {
                OnModSettingsApplied(value);
            };

            OnModSettingsApplied(entry.Value);
        }
        

        private void OnModSettingsApplied(bool isEnabled)
        {
            if (isEnabled == myWasEnabled) return;
            
            if (isEnabled)
                ApplyPatches();
            else
                UnpatchAll();
            
            Logger.Msg($"Finitizer is now {(isEnabled ? "enabled" : "disabled")}");

            myWasEnabled = isEnabled;
        }

        private unsafe void ApplyPatches()
        {
            if (myArePatchesApplied) return;
            
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_position_Injected), out ourOriginalTransformSetter, SetTransformVectorPatch);
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_rotation_Injected), out ourOriginalTransformRotSetter, SetTransformQuaternionPatch);
            PatchICall("UnityEngine.Transform::" + nameof(Transform.SetPositionAndRotation_Injected), out ourOriginalTransformTwinSetter, SetTransformVectorQuaternionPatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_position_Injected), out ourOriginalRigidbodyPosSetter, SetRigidbodyPosPatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_rotation_Injected), out ourOriginalRigidbodyRotSetter, SetRigidbodyRotPatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MovePosition_Injected), out ourOriginalRigidbodyPosMove, SetRigidbodyPosMovePatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MoveRotation_Injected), out ourOriginalRigidbodyRotMove, SetRigidbodyRotMovePatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_velocity_Injected), out ourOriginalRigidbodyVelSetter, SetRigidbodyVelPatch);
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_angularVelocity_Injected), out ourOriginalRigidbodyAvSetter, SetRigidbodyAvPatch);
            PatchICall("UnityEngine.Object::" + nameof(Object.Internal_InstantiateSingle_Injected), out ourOriginalInstantiateSimple, InstantiateSimplePatch);
            PatchICall("UnityEngine.Object::" + nameof(Object.Internal_InstantiateSingleWithParent_Injected), out ourOriginalInstantiateWithParent, InstantiateWithParentPatch);

            myArePatchesApplied = true;
            Logger.Msg("Things patching complete");
        }
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void VectorSetter(IntPtr instance, Vector3* vector);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void QuaternionSetter(IntPtr instance, Quaternion* vector);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void VectorQuaternionSetter(IntPtr instance, Vector3* vector, Quaternion* quat);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate IntPtr InstantiatorSimple(IntPtr target, Vector3* vector, Quaternion* quat);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate IntPtr InstantiatorWithParent(IntPtr target, IntPtr parent, Vector3* vector, Quaternion* quat);

        private static VectorSetter ourOriginalTransformSetter;
        private static QuaternionSetter ourOriginalTransformRotSetter;
        private static VectorQuaternionSetter ourOriginalTransformTwinSetter;
        private static VectorSetter ourOriginalRigidbodyPosSetter;
        private static QuaternionSetter ourOriginalRigidbodyRotSetter;
        private static VectorSetter ourOriginalRigidbodyPosMove;
        private static QuaternionSetter ourOriginalRigidbodyRotMove;
        private static VectorSetter ourOriginalRigidbodyAvSetter;
        private static VectorSetter ourOriginalRigidbodyVelSetter;
        private static InstantiatorSimple ourOriginalInstantiateSimple;
        private static InstantiatorWithParent ourOriginalInstantiateWithParent;

        private static void PatchICall<T>(string name, out T original, T target) where T: MulticastDelegate
        {
            var originalPointer = IL2CPP.il2cpp_resolve_icall(name);
            if (originalPointer == IntPtr.Zero)
            {
                Logger.Warning($"ICall {name} was not found, not patching");
                original = null;
                return;
            }

            NativePatchUtils.NativePatch(originalPointer, out original, target, name);
        }

        private void UnpatchAll()
        {
            if (!myArePatchesApplied) return;

            NativePatchUtils.UnpatchAll();

            myArePatchesApplied = false;
            Logger.Msg("Things unpatching complete");
        }
        
        public static unsafe bool IsInvalid(float f) => (*(int*) &f & int.MaxValue) >= 2139095040;

        private static unsafe void SetTransformVectorPatch(IntPtr instance, Vector3* vector)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;

            ourOriginalTransformSetter(instance, vector);
        }
        
        private static unsafe void SetTransformQuaternionPatch(IntPtr instance, Quaternion* quat)
        {
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            ourOriginalTransformRotSetter(instance, quat);
        }
        
        private static unsafe void SetTransformVectorQuaternionPatch(IntPtr instance, Vector3* vector, Quaternion* quat)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;
            
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            ourOriginalTransformTwinSetter(instance, vector, quat);
        }
        
        private static unsafe void SetRigidbodyPosPatch(IntPtr instance, Vector3* vector)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;

            ourOriginalRigidbodyPosSetter(instance, vector);
        }
        
        private static unsafe void SetRigidbodyRotPatch(IntPtr instance, Quaternion* quat)
        {
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            ourOriginalRigidbodyRotSetter(instance, quat);
        }
        
        private static unsafe void SetRigidbodyPosMovePatch(IntPtr instance, Vector3* vector)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;

            ourOriginalRigidbodyPosMove(instance, vector);
        }
        
        private static unsafe void SetRigidbodyRotMovePatch(IntPtr instance, Quaternion* quat)
        {
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            ourOriginalRigidbodyRotMove(instance, quat);
        }
        
        private static unsafe void SetRigidbodyAvPatch(IntPtr instance, Vector3* vector)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;

            ourOriginalRigidbodyAvSetter(instance, vector);
        }
        
        private static unsafe void SetRigidbodyVelPatch(IntPtr instance, Vector3* vector)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;

            ourOriginalRigidbodyVelSetter(instance, vector);
        }
        
        private static unsafe IntPtr InstantiateSimplePatch(IntPtr target, Vector3* vector, Quaternion* quat)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;
            
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            return ourOriginalInstantiateSimple(target, vector, quat);
        }
        
        private static unsafe IntPtr InstantiateWithParentPatch(IntPtr target, IntPtr parent, Vector3* vector, Quaternion* quat)
        {
            // All NaN comparisons are false, and infinities compare well
            if (!(vector->x > MaxAllowedValueBottom && vector->x < MaxAllowedValueTop)) vector->x = 0f;
            if (!(vector->y > MaxAllowedValueBottom && vector->y < MaxAllowedValueTop)) vector->y = 0f;
            if (!(vector->z > MaxAllowedValueBottom && vector->z < MaxAllowedValueTop)) vector->z = 0f;
            
            if(!(quat->x > MaxAllowedValueBottom && quat->x < MaxAllowedValueTop) || 
               !(quat->y > MaxAllowedValueBottom && quat->y < MaxAllowedValueTop) || 
               !(quat->z > MaxAllowedValueBottom && quat->z < MaxAllowedValueTop) || 
               !(quat->w > MaxAllowedValueBottom && quat->w < MaxAllowedValueTop))
            {
                quat->x = 0f;
                quat->y = 0f;
                quat->z = 0f;
                quat->w = 1f;
            }

            return ourOriginalInstantiateWithParent(target, parent, vector, quat);
        }
    }
}