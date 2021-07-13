using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IntegrityCheckWeaver
{
    static class Program
    {
        /// <summary>
        /// Why, you may ask?
        /// Because certain individuals with incredibly loose morals are hell-bent on making the game worse for everyone
        /// </summary>
        static int Main(string[] args)
        {
            if (args.Length <= 0 || !File.Exists(args[0]))
            {
                Console.Error.WriteLine("Bad arguments");
                return 1;
            }

            using var assembly = AssemblyDefinition.ReadAssembly(new FileStream(args[0], FileMode.Open, FileAccess.ReadWrite));
            var loaderCheckType = assembly.MainModule.GetType("LoaderIntegrityCheck");
            var customizedModType = assembly.MainModule.GetType("CustomizedMelonMod");
            var annoyingMessage = assembly.MainModule.GetType("AnnoyingMessagePrinter");

            if (loaderCheckType == null || customizedModType == null || annoyingMessage == null)
            {
                Console.Error.WriteLine("Required types not found");
                return 1;
            }

            loaderCheckType.Name = Utils.CompletelyRandomString();
            customizedModType.Name = Utils.CompletelyRandomString();
            annoyingMessage.Name = Utils.CompletelyRandomString();

            CleanMethods(loaderCheckType);
            CleanMethods(customizedModType);
            CleanMethods(annoyingMessage);
            
            var dummyOneResource = DummyThree.ProduceDummyThree();
            var dummyOneName = Utils.CompletelyRandomString() + ".dll";
            assembly.MainModule.Resources.Add(new EmbeddedResource(dummyOneName, ManifestResourceAttributes.Private, dummyOneResource));

            var dummyThreeResource = DummyThree.ProduceDummyThree();
            var dummyThreeName = Utils.CompletelyRandomString() + ".dll";
            assembly.MainModule.Resources.Add(new EmbeddedResource(dummyThreeName, ManifestResourceAttributes.Private, dummyThreeResource));

            var dummyTwoName = Utils.CompletelyRandomString() + ".dll";

            assembly.MainModule.Resources.Remove(assembly.MainModule.Resources.Single(it => it.Name.EndsWith("_dummy_.dll"))); // is replaced
            assembly.MainModule.Resources.Single(it => it.Name.EndsWith("_dummy2_.dll")).Name = dummyTwoName;
            
            foreach (var method in loaderCheckType.Methods)
            foreach (var instr in method.Body.Instructions)
                if (instr.OpCode == OpCodes.Ldstr)
                    instr.Operand = (string)instr.Operand switch
                    {
                        "_dummy_.dll" => dummyOneName,
                        "_dummy2_.dll" => dummyTwoName,
                        "_dummy3_.dll" => dummyThreeName,
                        _ => instr.Operand
                    };

            // todo: insert spicier checks!
            
            assembly.Write();

            return 0;
        }
        
        private static void CleanMethods(TypeDefinition type)
        {
            foreach (var method in type.Methods) 
                if (method.Name != ".ctor" && method.Name != ".cctor" && !method.IsVirtual)
                    method.Name = Utils.CompletelyRandomString();
            
            foreach (var property in type.Properties) 
                property.Name = Utils.CompletelyRandomString();

            foreach (var field in type.Fields) 
                field.Name = Utils.CompletelyRandomString();
        }
    }
}