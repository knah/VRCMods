using System;
using System.Runtime.InteropServices;

namespace Turbones
{

    [StructLayout(LayoutKind.Sequential)]
    internal struct ICallOffsets
    {
        public IntPtr get_transform_component;
        public IntPtr get_transform_ltw_matrix;
        public IntPtr get_component_enabled;
        public IntPtr set_transform_position_and_rotation;
        public IntPtr get_transform_child_count;

        public IntPtr gchandle_create;
        public IntPtr gchandle_drop;
        public IntPtr gchandle_get;

        public IntPtr transform_get_local_rotation;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct ColliderComponentOffsets {
        public uint collider_radius;
        public uint collider_height;
        public uint collider_center;
        public uint collider_bound;
        public uint collider_direction;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct ParticleClassOffsets {
        public uint transform;
        public uint parent_index;
        public uint damping;
        public uint elasticity;
        public uint stiffness;
        public uint inert;
        public uint radius;
        public uint end_offset;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct BoneComponentOffsets {
        public uint particle_list;
        public uint collider_list;
        public uint gravity;
        public uint force;
        public uint local_gravity;
        public uint freeze_axis;
        public uint update_rate;
        public uint root;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct ListClassOffsets
    {
        public uint size;
        public uint store;
        public uint array_store;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectOffsets
    {
        public uint cached_ptr;
    }
}