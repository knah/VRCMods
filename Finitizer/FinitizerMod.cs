using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Finitizer;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(FinitizerMod), "Finitizer", "1.2.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace Finitizer
{
    public class FinitizerMod : MelonMod
    {
        private const string SettingsCategory = "Finitizer";
        private const string EnabledSetting = "Enabled";

        private bool myArePatchesApplied;
        private bool myWasEnabled;
        
        public override void OnApplicationStart()
        {
            var category = MelonPreferences.CreateCategory(SettingsCategory, SettingsCategory);
            var entry = (MelonPreferences_Entry<bool>) category.CreateEntry(EnabledSetting, true, "FP fix enabled");
            entry.OnValueChanged += (old, value) =>
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
            
            MelonLogger.Msg($"Finitizer is now {(isEnabled ? "enabled" : "disabled")}");

            myWasEnabled = isEnabled;
        }

        private void ApplyPatches()
        {
            if (myArePatchesApplied) return;
            
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_position_Injected), out ourOriginalTransformSetter, nameof(SetTransformVectorPatch));
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_rotation_Injected), out ourOriginalTransformRotSetter, nameof(SetTransformQuaternionPatch));
            PatchICall("UnityEngine.Transform::" + nameof(Transform.SetPositionAndRotation_Injected), out ourOriginalTransformTwinSetter, nameof(SetTransformVectorQuaternionPatch));
            
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_position_Injected), out ourOriginalRigidbodyPosSetter, nameof(SetRigidbodyPosPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_rotation_Injected), out ourOriginalRigidbodyRotSetter, nameof(SetRigidbodyRotPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MovePosition_Injected), out ourOriginalRigidbodyPosMove, nameof(SetRigidbodyPosMovePatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MoveRotation_Injected), out ourOriginalRigidbodyRotMove, nameof(SetRigidbodyRotMovePatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_velocity_Injected), out ourOriginalRigidbodyVelSetter, nameof(SetRigidbodyVelPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_angularVelocity_Injected), out ourOriginalRigidbodyAvSetter, nameof(SetRigidbodyAvPatch));
            
            PatchICall("UnityEngine.Object::" + nameof(Object.Internal_InstantiateSingle_Injected), out ourOriginalInstantiateSimple, nameof(InstantiateSimplePatch));
            PatchICall("UnityEngine.Object::" + nameof(Object.Internal_InstantiateSingleWithParent_Injected), out ourOriginalInstantiateWithParent, nameof(InstantiateWithParentPatch));

            myArePatchesApplied = true;
            MelonLogger.Msg("Things patching complete");
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

        private static readonly Dictionary<string, (IntPtr, IntPtr)> ourOriginalPointers = new Dictionary<string, (IntPtr, IntPtr)>();

        private static unsafe void PatchICall<T>(string name, out T original, string patchName) where T: MulticastDelegate
        {
            var originalPointer = IL2CPP.il2cpp_resolve_icall(name);
            if (originalPointer == IntPtr.Zero)
            {
                MelonLogger.LogWarning($"ICall {name} was not found, not patching");
                original = null;
                return;
            }

            var target = typeof(FinitizerMod).GetMethod(patchName, BindingFlags.Static | BindingFlags.NonPublic);
            var functionPointer = target!.MethodHandle.GetFunctionPointer();

            Imports.Hook((IntPtr) (&originalPointer), functionPointer);
            
            ourOriginalPointers[name] = (originalPointer, functionPointer);

            original = Marshal.GetDelegateForFunctionPointer<T>(originalPointer);
        }

        private unsafe void UnpatchAll()
        {
            if (!myArePatchesApplied) return;

            foreach (var keyValuePair in ourOriginalPointers)
            {
                var pointer = keyValuePair.Value.Item1;
                Imports.Unhook((IntPtr) (&pointer), keyValuePair.Value.Item2);
            }

            ourOriginalPointers.Clear();

            myArePatchesApplied = false;
            MelonLogger.Log("Things unpatching complete");
        }
        
        public static unsafe bool IsInvalid(float f) => (*(int*) &f & int.MaxValue) >= 2139095040;

        private static unsafe void SetTransformVectorPatch(IntPtr instance, Vector3* vector)
        {
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;

            ourOriginalTransformSetter(instance, vector);
        }
        
        private static unsafe void SetTransformQuaternionPatch(IntPtr instance, Quaternion* quat)
        {
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;
            
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;

            ourOriginalRigidbodyPosSetter(instance, vector);
        }
        
        private static unsafe void SetRigidbodyRotPatch(IntPtr instance, Quaternion* quat)
        {
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;

            ourOriginalRigidbodyPosMove(instance, vector);
        }
        
        private static unsafe void SetRigidbodyRotMovePatch(IntPtr instance, Quaternion* quat)
        {
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;

            ourOriginalRigidbodyAvSetter(instance, vector);
        }
        
        private static unsafe void SetRigidbodyVelPatch(IntPtr instance, Vector3* vector)
        {
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;

            ourOriginalRigidbodyVelSetter(instance, vector);
        }
        
        private static unsafe IntPtr InstantiateSimplePatch(IntPtr target, Vector3* vector, Quaternion* quat)
        {
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;
            
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;
            
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->y & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->z & int.MaxValue) >= 2139095040 ||
                (*(int*) &quat->w & int.MaxValue) >= 2139095040)
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