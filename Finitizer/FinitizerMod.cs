using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Finitizer;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;
using PhotonSerializers = ObjectPublicAbstractSealedSiDiSi2ObSiObSiSiSiUnique;

[assembly:MelonInfo(typeof(FinitizerMod), "Finitizer", "1.0.0", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace Finitizer
{
    public class FinitizerMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            MelonCoroutines.Start(InitThings());
        }

        public IEnumerator InitThings()
        {
            while (VRCUiManager.field_Protected_Static_VRCUiManager_0 == null)
                yield return null;

            ApplyPatches();
        }

        private static void ApplyPatches()
        {
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_position_Injected), out ourOriginalTransformSetter, nameof(SetTransformVectorPatch));
            PatchICall("UnityEngine.Transform::" + nameof(Transform.set_rotation_Injected), out ourOriginalTransformRotSetter, nameof(SetTransformQuaternionPatch));
            PatchICall("UnityEngine.Transform::" + nameof(Transform.SetPositionAndRotation_Injected), out ourOriginalTransformTwinSetter, nameof(SetTransformVectorQuaternionPatch));
            
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_position_Injected), out ourOriginalRigidbodyPosSetter, nameof(SetRigidbodyPosPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_rotation_Injected), out ourOriginalRigidbodyRotSetter, nameof(SetRigidbodyRotPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MovePosition_Injected), out ourOriginalRigidbodyPosMove, nameof(SetRigidbodyPosMovePatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.MoveRotation_Injected), out ourOriginalRigidbodyRotMove, nameof(SetRigidbodyRotMovePatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_velocity_Injected), out ourOriginalRigidbodyVelSetter, nameof(SetRigidbodyVelPatch));
            PatchICall("UnityEngine.Rigidbody::" + nameof(Rigidbody.set_angularVelocity_Injected), out ourOriginalRigidbodyAvSetter, nameof(SetRigidbodyAvPatch));
            
            // These two were originally used to deserialize stuff from photon, but it seems like the vector one is not used anymore
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(PhotonSerializers).GetMethods(BindingFlags.Static | BindingFlags.Public |BindingFlags.DeclaredOnly).Single(it =>
                    {
                        var parameters = it.GetParameters();
                        if (it.ReturnType != typeof(void) || parameters.Length != 3 || it.Name.Contains("_PDM_") || parameters[1].ParameterType != typeof(Vector3).MakeByRefType())
                            return false;
                        return true;
                    }))
                    .GetValue(null);
                
                Imports.Hook((IntPtr)(&originalMethodPointer), typeof(FinitizerMod).GetMethod(nameof(VectorPatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourOriginalVectorMethod = Marshal.GetDelegateForFunctionPointer<VectorMethod>(originalMethodPointer);
            }
            
            unsafe
            {
                var originalMethodPointer = *(IntPtr*) (IntPtr) UnhollowerUtils
                    .GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(PhotonSerializers).GetMethods(BindingFlags.Static | BindingFlags.Public |BindingFlags.DeclaredOnly).Single(it =>
                    {
                        var parameters = it.GetParameters();
                        if (it.ReturnType != typeof(void) || parameters.Length != 3 || it.Name.Contains("_PDM_") || parameters[1].ParameterType != typeof(Quaternion).MakeByRefType())
                            return false;
                        return true;
                    }))
                    .GetValue(null);
                
                Imports.Hook((IntPtr)(&originalMethodPointer), typeof(FinitizerMod).GetMethod(nameof(QuaternionPatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());

                ourOriginalQuaternionMethod = Marshal.GetDelegateForFunctionPointer<QuaternionMethod>(originalMethodPointer);
            }
            
            MelonLogger.Log("Things patching complete");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void VectorMethod(IntPtr arg1, Vector3* vector, int enumValue);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void QuaternionMethod(IntPtr arg1, Quaternion* quat, int enumValue);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void VectorSetter(IntPtr instance, Vector3* vector);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void QuaternionSetter(IntPtr instance, Quaternion* vector);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void VectorQuaternionSetter(IntPtr instance, Vector3* vector, Quaternion* quat);

        private static VectorMethod ourOriginalVectorMethod;
        private static QuaternionMethod ourOriginalQuaternionMethod;
        private static VectorSetter ourOriginalTransformSetter;
        private static QuaternionSetter ourOriginalTransformRotSetter;
        private static VectorQuaternionSetter ourOriginalTransformTwinSetter;
        private static VectorSetter ourOriginalRigidbodyPosSetter;
        private static QuaternionSetter ourOriginalRigidbodyRotSetter;
        private static VectorSetter ourOriginalRigidbodyPosMove;
        private static QuaternionSetter ourOriginalRigidbodyRotMove;
        private static VectorSetter ourOriginalRigidbodyAvSetter;
        private static VectorSetter ourOriginalRigidbodyVelSetter;

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
            Imports.Hook((IntPtr) (&originalPointer), target!.MethodHandle.GetFunctionPointer());

            original = Marshal.GetDelegateForFunctionPointer<T>(originalPointer);
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
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040) quat->x = 0f;
            if ((*(int*) &quat->y & int.MaxValue) >= 2139095040) quat->y = 0f;
            if ((*(int*) &quat->z & int.MaxValue) >= 2139095040) quat->z = 0f;
            if ((*(int*) &quat->w & int.MaxValue) >= 2139095040) quat->w = 1f;

            ourOriginalTransformRotSetter(instance, quat);
        }
        
        private static unsafe void SetTransformVectorQuaternionPatch(IntPtr instance, Vector3* vector, Quaternion* quat)
        {
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;
            
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040) quat->x = 0f;
            if ((*(int*) &quat->y & int.MaxValue) >= 2139095040) quat->y = 0f;
            if ((*(int*) &quat->z & int.MaxValue) >= 2139095040) quat->z = 0f;
            if ((*(int*) &quat->w & int.MaxValue) >= 2139095040) quat->w = 1f;

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
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040) quat->x = 0f;
            if ((*(int*) &quat->y & int.MaxValue) >= 2139095040) quat->y = 0f;
            if ((*(int*) &quat->z & int.MaxValue) >= 2139095040) quat->z = 0f;
            if ((*(int*) &quat->w & int.MaxValue) >= 2139095040) quat->w = 1f;

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
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040) quat->x = 0f;
            if ((*(int*) &quat->y & int.MaxValue) >= 2139095040) quat->y = 0f;
            if ((*(int*) &quat->z & int.MaxValue) >= 2139095040) quat->z = 0f;
            if ((*(int*) &quat->w & int.MaxValue) >= 2139095040) quat->w = 1f;

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
        
        private static unsafe void VectorPatch(IntPtr arg1, Vector3* vector, int enumValue)
        {
            ourOriginalVectorMethod(arg1, vector, enumValue);
            
            if ((*(int*) &vector->x & int.MaxValue) >= 2139095040) vector->x = 0f;
            if ((*(int*) &vector->y & int.MaxValue) >= 2139095040) vector->y = 0f;
            if ((*(int*) &vector->z & int.MaxValue) >= 2139095040) vector->z = 0f;
        }
        
        private static unsafe void QuaternionPatch(IntPtr arg1, Quaternion* quat, int enumValue)
        {
            ourOriginalQuaternionMethod(arg1, quat, enumValue);
            
            if ((*(int*) &quat->x & int.MaxValue) >= 2139095040) quat->x = 0f;
            if ((*(int*) &quat->y & int.MaxValue) >= 2139095040) quat->y = 0f;
            if ((*(int*) &quat->z & int.MaxValue) >= 2139095040) quat->z = 0f;
            if ((*(int*) &quat->w & int.MaxValue) >= 2139095040) quat->w = 1f;
        }
    }
}