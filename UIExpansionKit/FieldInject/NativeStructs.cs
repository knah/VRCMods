using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Runtime;

namespace UIExpansionKit.FieldInject
{
    // These are copypasted from Unhollower. TODO: use actual structs from unhollower once/if they become public
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Il2CppClass_24_2
    {
        // The following fields are always valid for a Il2CppClass structure
        public Il2CppImage* image; // const
        public IntPtr gc_desc;
        public IntPtr name; // const char*
        public IntPtr namespaze; // const char*
        public Il2CppType_16_0 byval_arg; // not const, no ptr
        public Il2CppType_16_0 this_arg; // not const, no ptr
        public Il2CppClass* element_class; // not const
        public Il2CppClass* castClass; // not const
        public Il2CppClass* declaringType; // not const
        public Il2CppClass* parent; // not const
        public /*Il2CppGenericClass**/ IntPtr generic_class;

        public /*Il2CppTypeDefinition**/
            IntPtr typeDefinition; // const; non-NULL for Il2CppClass's constructed from type defintions

        public /*Il2CppInteropData**/ IntPtr interopData; // const

        public Il2CppClass_24_2* klass; // not const; hack to pretend we are a MonoVTable. Points to ourself
        // End always valid fields

        // The following fields need initialized before access. This can be done per field or as an aggregate via a call to Class::Init
        public Il2CppFieldInfo_24_1* fields; // Initialized in SetupFields
        public Il2CppEventInfo* events; // const; Initialized in SetupEvents
        public Il2CppPropertyInfo* properties; // const; Initialized in SetupProperties
        public Il2CppMethodInfo** methods; // const; Initialized in SetupMethods
        public Il2CppClass_24_2** nestedTypes; // not const; Initialized in SetupNestedTypes
        public Il2CppClass_24_2** implementedInterfaces; // not const; Initialized in SetupInterfaces
        public Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets; // not const; Initialized in Init
        public IntPtr static_fields; // not const; Initialized in Init

        public /*Il2CppRGCTXData**/ IntPtr rgctx_data; // const; Initialized in Init

        // used for fast parent checks
        public Il2CppClass_24_2** typeHierarchy; // not const; Initialized in SetupTypeHierachy
        // End initialization required fields

        // U2019 specific field
        public IntPtr unity_user_data;

        public uint initializationExceptionGCHandle;

        public uint cctor_started;

        public uint cctor_finished;

        /*ALIGN_TYPE(8)*/
        private IntPtr cctor_thread; // was uint64 in 2018.4, is size_t in 2019.3.1

        // Remaining fields are always valid except where noted
        public /*GenericContainerIndex*/ int genericContainerIndex;
        public uint instance_size;
        public uint actualSize;
        public uint element_size;
        public int native_size;
        public uint static_fields_size;
        public uint thread_static_fields_size;
        public int thread_static_fields_offset;
        public Il2CppClassAttributes flags;
        public uint token;

        public ushort method_count; // lazily calculated for arrays, i.e. when rank > 0
        public ushort property_count;
        public ushort field_count;
        public ushort event_count;
        public ushort nested_type_count;
        public ushort vtable_count; // lazily calculated for arrays, i.e. when rank > 0
        public ushort interfaces_count;
        public ushort interface_offsets_count; // lazily calculated for arrays, i.e. when rank > 0

        public byte typeHierarchyDepth; // Initialized in SetupTypeHierachy
        public byte genericRecursionDepth;
        public byte rank;
        public byte minimumAlignment; // Alignment of this type
        public byte naturalAligment; // Alignment of this type without accounting for packing
        public byte packingSize;

        // this is critical for performance of Class::InitFromCodegen. Equals to initialized && !has_initialization_error at all times.
        // Use Class::UpdateInitializedAndNoError to update
        public byte bitfield_1;
        /*uint8_t initialized_and_no_error : 1;

        uint8_t valuetype : 1;
        uint8_t initialized : 1;
        uint8_t enumtype : 1;
        uint8_t is_generic : 1;
        uint8_t has_references : 1;
        uint8_t init_pending : 1;
        uint8_t size_inited : 1;*/

        public byte bitfield_2;
        /*uint8_t has_finalize : 1;
        uint8_t has_cctor : 1;
        uint8_t is_blittable : 1;
        uint8_t is_import_or_windows_runtime : 1;
        uint8_t is_vtable_initialized : 1;
        uint8_t has_initialization_error : 1;*/

        //VirtualInvokeData vtable[IL2CPP_ZERO_LEN_ARRAY];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Il2CppType_16_0
    {
        public IntPtr data;
        public ushort attrs;
        public Il2CppTypeEnum type;
        public byte mods_byref_pin;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Il2CppFieldInfo_24_1
    {
        public IntPtr name; // const char*
        public Il2CppType_16_0* type; // const
        public Il2CppClass_24_2* parent; // non-const?
        public int offset; // If offset is -1, then it's thread static
        public uint token;
    }
}