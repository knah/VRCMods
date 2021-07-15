using System;
using System.Collections.Generic;
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
            var modType = assembly.MainModule.Types.SingleOrDefault(it => it.BaseType?.Name == "MelonMod");

            if (modType == null)
            {
                Console.Error.WriteLine("Required types not found");
                return 1;
            }

            var dummyOneResource = DummyThree.ProduceDummyThree();
            var dummyOneName = Utils.CompletelyRandomString() + ".dll";
            assembly.MainModule.Resources.Add(new EmbeddedResource(dummyOneName, ManifestResourceAttributes.Private, dummyOneResource));

            var dummyThreeResource = DummyThree.ProduceDummyThree();
            var dummyThreeName = Utils.CompletelyRandomString() + ".dll";
            assembly.MainModule.Resources.Add(new EmbeddedResource(dummyThreeName, ManifestResourceAttributes.Private, dummyThreeResource));

            var dummyTwoName = Utils.CompletelyRandomString() + ".dll";

            assembly.MainModule.Resources.Remove(assembly.MainModule.Resources.Single(it => it.Name.EndsWith("_dummy_.dll"))); // is replaced
            assembly.MainModule.Resources.Single(it => it.Name.EndsWith("_dummy2_.dll")).Name = dummyTwoName;
            
            var methodRenameMap = CleanMethods(modType);

            foreach (var method in modType.Methods)
            foreach (var instr in method.Body.Instructions)
                if (instr.OpCode == OpCodes.Ldstr)
                {
                    var value = (string)instr.Operand;
                    instr.Operand = value switch
                    {
                        "_dummy_.dll" => dummyOneName,
                        "_dummy2_.dll" => dummyTwoName,
                        "_dummy3_.dll" => dummyThreeName,
                        _ => methodRenameMap.TryGetValue(value, out var renamed) ? renamed : value
                    };
                }

            assembly.Write();

            return 0;
        }

        private static readonly HashSet<string> ourNamesToRename = new()
        {
            "CheckA",
            "CheckB",
            "CheckC",
            "PatchTest",
            "ReturnFalse",
            "CheckWasSuccessful",
            "MustStayFalse",
            "MustStayTrue",
            "RanCheck3",
            "CheckDummyThree",
            "ourAnnoyingMessages"
        };

        private static Dictionary<string, string> CleanMethods(TypeDefinition type)
        {
            var result = new Dictionary<string, string>();

            foreach (var method in type.Methods)
                if (ourNamesToRename.Contains(method.Name))
                {
                    var newName = Utils.CompletelyRandomString();
                    result[method.Name] = newName;
                    method.Name = newName;
                }

            foreach (var property in type.Properties) 
                if (ourNamesToRename.Contains(property.Name))
                    property.Name = Utils.CompletelyRandomString();

            foreach (var field in type.Fields) 
                if (ourNamesToRename.Contains(field.Name))
                    field.Name = Utils.CompletelyRandomString();

            return result;
        }
    }
}