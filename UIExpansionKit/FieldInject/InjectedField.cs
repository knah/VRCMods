using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MelonLoader;
using UnhollowerBaseLib;

namespace UIExpansionKit.FieldInject
{
    public abstract class InjectedField<TOwner, TField> where TOwner: Il2CppObjectBase
    {
        public readonly string Name;
        private readonly int myOffset;

        protected InjectedField(string name)
        {
            Name = name;
            var classPointer = Il2CppClassPointerStore<TOwner>.NativeClassPtr;
            if (classPointer == IntPtr.Zero)
                throw new ArgumentException($"Type {typeof(TOwner)} is not an il2cpp type!");
            
            if (!typeof(TField).IsValueType && !typeof(Il2CppObjectBase).IsAssignableFrom(typeof(TField)) && typeof(TField) != typeof(string))
                throw new ArgumentException($"Type {typeof(TField)} can't be used in IL2CPP!");

            var fieldTypePointer = Il2CppClassPointerStore<TField>.NativeClassPtr;
            if (fieldTypePointer == IntPtr.Zero)
                throw new ArgumentException("Type {typeof(TField)} can't be used in IL2CPP (no class pointer)!");

            unsafe
            {
                var addedSize = (uint) (typeof(TField).IsValueType ? Marshal.SizeOf<TField>() : IntPtr.Size);
                var ownerClass = (Il2CppClass_24_2*) classPointer;

                myOffset = (int)ownerClass->instance_size - IntPtr.Size;
                UiExpansionKitMod.Instance.Logger.Msg($"Injecting field: current size {ownerClass->instance_size} added size {addedSize} offset {myOffset}");

                var fieldType = (Il2CppType_16_0*)Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppType_16_0>());
                *fieldType = ((Il2CppClass_24_2*)fieldTypePointer)->byval_arg;
                fieldType->attrs = 6;
                fieldType->mods_byref_pin = 0;

                var newFieldInfo = new Il2CppFieldInfo_24_1
                {
                    name = Marshal.StringToHGlobalAnsi(name),
                    offset = myOffset,
                    parent = ownerClass,
                    type = fieldType,
                    token = 0,
                };

                var newFieldInfoArray = (Il2CppFieldInfo_24_1*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppFieldInfo_24_1>() * (ownerClass->field_count + 1));
                for (var i = 0; i < ownerClass->field_count; i++) 
                    newFieldInfoArray[i] = ownerClass->fields[i];

                newFieldInfoArray[ownerClass->field_count] = newFieldInfo;

                // This leaks the previous array, but who cares
                ownerClass->fields = newFieldInfoArray;
                ownerClass->instance_size += addedSize;
                ownerClass->actualSize += addedSize;
                ownerClass->field_count++;

                ownerClass->bitfield_1 |= 1 << 5; // has_references

                // update GC descriptor so that reference fields are not lost
                //ownerClass->gc_desc = (IntPtr)(ownerClass->instance_size & ~3L);
            }
        }

        protected IntPtr GetPointer(IntPtr objectPointer)
        {
            if (objectPointer == IntPtr.Zero) return IntPtr.Zero;
            return objectPointer + myOffset;
        }
    }

    public class StringInjectedField<TOwner> : InjectedField<TOwner, string> where TOwner : Il2CppObjectBase
    {
        public StringInjectedField(string name) : base(name)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string Get(TOwner owner) => IL2CPP.Il2CppStringToManaged(*(IntPtr*)GetPointer(owner.Pointer));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set(TOwner owner, string value) => *(IntPtr*)GetPointer(owner.Pointer) = IL2CPP.ManagedStringToIl2Cpp(value);
    }

    public class StructInjectedField<TOwner, TField> : InjectedField<TOwner, TField>
        where TOwner : Il2CppObjectBase 
        where TField : unmanaged
    {
        public StructInjectedField(string name) : base(name)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref TField Ref(TOwner owner) => ref *(TField*) GetPointer(owner.Pointer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe TField Get(TOwner owner) => *(TField*) GetPointer(owner.Pointer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set(TOwner owner, TField value) => *(TField*) GetPointer(owner.Pointer) = value;
    }

    public class RefInjectedField<TOwner, TField> : InjectedField<TOwner, TField>
        where TOwner : Il2CppObjectBase
        where TField : Il2CppObjectBase
    {
        public RefInjectedField(string name) : base(name)
        {
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe TField Get(TOwner owner)
        {
            var objectPtr = *(IntPtr*)GetPointer(owner.Pointer);
            if (objectPtr == IntPtr.Zero) return null;

            return (TField) Activator.CreateInstance(typeof(TField), objectPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Set(TOwner owner, TField value) => *(IntPtr*) GetPointer(owner.Pointer) = value?.Pointer ?? IntPtr.Zero;
    }
}