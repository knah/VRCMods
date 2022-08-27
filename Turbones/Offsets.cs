using System;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;

namespace Turbones
{
    public static class Offsets
    {
        public static IntPtr GetICall(string name)
        {
            var result = IL2CPP.il2cpp_resolve_icall(name);
            if (result == IntPtr.Zero) TurbonesMod.Logger.Error($"ICall {name} not found, crash will likely follow");
            
            MelonDebug.Msg($"ICall pointer for {name} is {result}");
            return result;
        }

        public static uint GetFieldOffset<T>(string fieldName)
        {
            var classPtr = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (classPtr == IntPtr.Zero)
            {
                TurbonesMod.Logger.Error($"{typeof(T)} class pointer is null");
                return 0;
            }

            var fieldPtr = IL2CPP.il2cpp_class_get_field_from_name(classPtr, fieldName);
            if (fieldPtr == IntPtr.Zero)
            {
                var managedField = typeof(T).GetField("NativeFieldInfoPtr_" + fieldName, BindingFlags.Static | BindingFlags.NonPublic);
                if (managedField == null)
                {
                    TurbonesMod.Logger.Error($"Field {fieldName} is not found on type {typeof(T)} (managed)");
                    return 0;
                }
                fieldPtr = (IntPtr) managedField.GetValue(null);

                if (fieldPtr == IntPtr.Zero)
                {
                    TurbonesMod.Logger.Error($"Field {fieldName} is not found on type {typeof(T)} (ptr)");
                    return 0;
                }
            }

            var fieldOffset = IL2CPP.il2cpp_field_get_offset(fieldPtr);
            if (fieldOffset <= 0) 
                TurbonesMod.Logger.Error($"Field offset for field {typeof(T)}.{fieldName} is {fieldOffset}");
            
            MelonDebug.Msg($"Field offset for field {typeof(T)}.{fieldName} is {fieldOffset}");

            return fieldOffset;
        }

        private static IntPtr GetProcAddressSafe(IntPtr module, string name)
        {
            if (module == IntPtr.Zero) return IntPtr.Zero;
            var result = JigglySolverApi.GetProcAddress(module, name);
            if (result == IntPtr.Zero)
                TurbonesMod.Logger.Error($"No entry point for {name}");
            else
                MelonDebug.Msg($"Entry point for {name} is {result}");

            return result;
        }

        public static void SetOffsets()
        {
            var ga = JigglySolverApi.LoadLibraryA("GameAssembly.dll");

            if (ga == IntPtr.Zero)
            {
                TurbonesMod.Logger.Error("GameAssembly was not found, crash will likely follow");
            }

            var icalls = new ICallOffsets
            {
                get_transform_component = GetICall("UnityEngine.Component::get_transform"),
                get_transform_ltw_matrix = GetICall("UnityEngine.Transform::get_localToWorldMatrix_Injected"),
                get_component_enabled = GetICall("UnityEngine.Behaviour::get_enabled"),
                get_transform_child_count = GetICall("UnityEngine.Transform::get_childCount"),
                set_transform_position_and_rotation = GetICall("UnityEngine.Transform::SetPositionAndRotation_Injected"),
                
                gchandle_create = GetProcAddressSafe(ga, nameof(IL2CPP.il2cpp_gchandle_new)),
                gchandle_drop = GetProcAddressSafe(ga, nameof(IL2CPP.il2cpp_gchandle_free)),
                gchandle_get = GetProcAddressSafe(ga, nameof(IL2CPP.il2cpp_gchandle_get_target)),
                
                transform_get_local_rotation = GetICall("UnityEngine.Transform::get_localRotation_Injected")
            };

            var objectOffsets = new ObjectOffsets
            {
                cached_ptr = GetFieldOffset<UnityEngine.Object>(nameof(UnityEngine.Object.m_CachedPtr)),
            };

            var colliderOffsets = new ColliderComponentOffsets
            {
                collider_radius = GetFieldOffset<DynamicBoneCollider>(nameof(DynamicBoneCollider.m_Radius)),
                collider_center = GetFieldOffset<DynamicBoneCollider>(nameof(DynamicBoneCollider.m_Center)),
                collider_bound = GetFieldOffset<DynamicBoneCollider>(nameof(DynamicBoneCollider.m_Bound)),
                collider_direction = GetFieldOffset<DynamicBoneCollider>(nameof(DynamicBoneCollider.m_Direction)),
                collider_height = GetFieldOffset<DynamicBoneCollider>(nameof(DynamicBoneCollider.m_Height)),
            };

            var listOffsets = new ListClassOffsets
            {
                size = GetFieldOffset<List<Il2CppSystem.Object>>(nameof(List<int>._size)),
                store = GetFieldOffset<List<Il2CppSystem.Object>>(nameof(List<int>._items)),
                array_store = (uint) IntPtr.Size * 4 // classPtr, monitor, bounds, maxSize
            };

            var boneOffsets = new BoneComponentOffsets
            {
                collider_list = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_Colliders)),
                particle_list = GetFieldOffset<DynamicBone>(nameof(DynamicBone.field_Private_List_1_ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique_0)),
                force = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_Force)),
                gravity = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_Gravity)),
                local_gravity = GetFieldOffset<DynamicBone>(nameof(DynamicBone.field_Private_Vector3_0)),
                freeze_axis = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_FreezeAxis)),
                update_rate = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_UpdateRate)),
                root = GetFieldOffset<DynamicBone>(nameof(DynamicBone.m_Root)),
                
            };

            var particleOffsets = new ParticleClassOffsets
            {
                transform = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Transform_0)),
                parent_index = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Int32_0)),
                
                damping = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Single_0)),
                elasticity = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Single_1)),
                stiffness = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Single_2)),
                inert = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Single_3)),
                radius = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Single_4)),
                
                end_offset = GetFieldOffset<DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique>(nameof(DynamicBone.ObjectNPrivateTrInSiVeSiQuVeSiVeSiUnique.field_Public_Vector3_2)),
            };

            JigglySolverApi.SetPointersAndOffsets(ref icalls, ref colliderOffsets, ref boneOffsets, ref particleOffsets,
                ref listOffsets, ref objectOffsets);
        }
    }
}