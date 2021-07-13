using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IntegrityCheckWeaver
{
    public static class DummyThree
    {
        public static byte[] ProduceDummyThree()
        {
            var assemblyName = new AssemblyNameDefinition(Utils.CompletelyRandomString(), new Version(1, 0, 0));
            var assembly = AssemblyDefinition.CreateAssembly(assemblyName, assemblyName.Name + ".dll", ModuleKind.Dll);

            var moduleType = assembly.MainModule.GetType("<Module>");

            void AddFunnyDelegate()
            {
                var dgType = new TypeDefinition("", Utils.CompletelyRandomString(), TypeAttributes.Sealed, assembly.MainModule.ImportReference(typeof(MulticastDelegate)));
                var invokeMethod = new MethodDefinition("Invoke", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot |
                                                                  MethodAttributes.Public, assembly.MainModule.ImportReference(typeof(void)));
                invokeMethod.ImplAttributes = MethodImplAttributes.CodeTypeMask;
                dgType.Methods.Add(invokeMethod);
                
                assembly.MainModule.Types.Add(dgType);
                
                dgType.Methods.Add(MakeCctor(assembly));
                
                // Try triggering type loads
                moduleType.Fields.Add(new FieldDefinition(Utils.CompletelyRandomString(), FieldAttributes.Private | FieldAttributes.Static, dgType));
                dgType.Fields.Add(new FieldDefinition(Utils.CompletelyRandomString(), FieldAttributes.Private | FieldAttributes.Static, dgType));
            }

            for (var i = 0; i < Utils.RandomInt(5, 30); i++)
                AddFunnyDelegate();
            
            moduleType.Methods.Add(MakeCctor(assembly));

            var memoryStream = new MemoryStream();
            
            assembly.Write(memoryStream);

            return memoryStream.ToArray();
        }

        private static MethodDefinition MakeCctor(AssemblyDefinition assembly)
        {
            var voidReference = assembly.MainModule.ImportReference(typeof(void));
            var cctorMethod = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                                                             MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, voidReference);
            var cctorBuilder = cctorMethod.Body.GetILProcessor();
            
            cctorBuilder.Emit(OpCodes.Ldc_I4_1);
            cctorBuilder.Emit(OpCodes.Conv_I);
            cctorBuilder.Emit(OpCodes.Calli, new CallSite(voidReference));
            cctorBuilder.Emit(OpCodes.Ret);

            return cctorMethod;
        }
    }
}