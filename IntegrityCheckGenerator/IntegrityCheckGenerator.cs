using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace IntegrityCheckGenerator
{
    [Generator]
    class IntegrityCheckGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor ourGenerationFailed = new("ICG0000",
            "Generation failed", "{0}", "Generators", DiagnosticSeverity.Error, true); 
        
        public void Initialize(GeneratorInitializationContext context)
        {
        
        }

        public void Execute(GeneratorExecutionContext context)
        {
            string? modTypeName = null;
            string? modNamespace = null;

            foreach (var tree in context.Compilation.SyntaxTrees)
            foreach (var decl in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var baseTypeName = decl.BaseList?.Types.FirstOrDefault()?.Type.ToString();
                if (baseTypeName != "MelonMod") continue;

                if (decl.Modifiers.All(it => it.ValueText != "partial"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, decl.GetLocation(), "Mod is not partial"));
                    continue;
                }

                if (modTypeName != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, decl.GetLocation(), "Too many mods in one project"));
                    continue;
                }

                modTypeName = decl.Identifier.ToString();
                var symbol = context.Compilation.GetSemanticModel(tree).GetDeclaredSymbol(decl);
                modNamespace = symbol!.ContainingNamespace.ToDisplayString();
                // hasSceneLoadedDerivative = symbol;
            }

            if (modTypeName == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ourGenerationFailed, null, "Too many mods in one project"));
                return;
            }

            var generatedCode = new StringBuilder();

            generatedCode.AppendLine("using HarmonyLib;");
            generatedCode.AppendLine("using System;");
            generatedCode.AppendLine("using System.Collections;");
            generatedCode.AppendLine("using System.IO;");
            generatedCode.AppendLine("using System.Linq;");
            generatedCode.AppendLine("using System.Reflection;");
            generatedCode.AppendLine("using System.Runtime.InteropServices;");
            generatedCode.AppendLine("using MelonLoader;");
            generatedCode.AppendLine("using UnityEngine;");
            
            generatedCode.AppendLine($"namespace {modNamespace} {{");
            generatedCode.AppendLine("[PatchShield]");
            generatedCode.AppendLine($"partial class {modTypeName} {{");
        
            generatedCode.AppendLine("internal static bool CheckWasSuccessful;");
            generatedCode.AppendLine("internal static bool MustStayFalse = false;");
            generatedCode.AppendLine("internal static bool MustStayTrue = true;");
            generatedCode.AppendLine("internal static bool RanCheck3 = false;");
            generatedCode.AppendLine("private static readonly Func<VRCUiManager> ourGetUiManager;");

            generatedCode.AppendLine($"static {modTypeName}() {{");
            generatedCode.AppendLine("try { if(typeof(MelonMod).Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product.IndexOf(\"free\", StringComparison.OrdinalIgnoreCase) != -1) {");
            PrintCheckFailedCode(generatedCode, 1);
            generatedCode.AppendLine("} } catch {");
            PrintCheckFailedCode(generatedCode, 1);
            generatedCode.AppendLine("}");
            generatedCode.AppendLine("CheckA();");
            generatedCode.AppendLine("var mm = typeof(MelonUtils).GetMethod(\"ToggleObfuscation\"); if(mm != null) { mm.Invoke(null, null); ");
            generatedCode.AppendLine("CheckA(); }");
            generatedCode.AppendLine("CheckB();");
            generatedCode.AppendLine("ourGetUiManager = (Func<VRCUiManager>) Delegate.CreateDelegate(typeof(Func<VRCUiManager>), typeof(VRCUiManager)");
            generatedCode.AppendLine("    .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)");
            generatedCode.AppendLine("    .First(it => it.PropertyType == typeof(VRCUiManager)).GetMethod);");
            generatedCode.AppendLine("CheckC();");
            generatedCode.AppendLine("CheckWasSuccessful = true;");
            generatedCode.AppendLine("}");
        
            generatedCode.AppendLine("partial void OnSceneWasLoaded2(int buildIndex, string sceneName);");
            generatedCode.AppendLine("public override void OnSceneWasLoaded(int buildIndex, string sceneName)");
            generatedCode.AppendLine("{");
            generatedCode.AppendLine("    if (buildIndex != -1 || RanCheck3) return;");
            generatedCode.AppendLine("    ");
            generatedCode.AppendLine("    try {");
            generatedCode.AppendLine("        var harmony = new HarmonyLib.Harmony(Guid.NewGuid().ToString());");
            generatedCode.AppendLine($"        harmony.Patch(AccessTools.Method(typeof({modTypeName}), nameof(PatchTast)),");
            generatedCode.AppendLine($"            new HarmonyMethod(typeof({modTypeName}), nameof(ReturnFalse)));");
            generatedCode.AppendLine("        PatchTast();");
            PrintCheckFailedCode(generatedCode, 2);
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("    catch (BadImageFormatException) {}");
            generatedCode.AppendLine("    finally { CheckDummyThree(); }");
            generatedCode.AppendLine("    RanCheck3 = true;");
            generatedCode.AppendLine("}");
        
            generatedCode.AppendLine($"protected {modTypeName}() {{");
            generatedCode.AppendLine("    if (CheckWasSuccessful && !MustStayFalse && MustStayTrue) return;");
            generatedCode.AppendLine("    ");
            PrintCheckFailedCode(generatedCode, 1);
            generatedCode.AppendLine("}");

            generatedCode.AppendLine("internal static VRCUiManager GetUiManager() => ourGetUiManager();");
        
            generatedCode.AppendLine("private static void DoAfterUiManagerInit(Action code) {");
            generatedCode.AppendLine("    MelonCoroutines.Start(OnUiManagerInitCoro(code));");
            generatedCode.AppendLine("}");

            generatedCode.AppendLine("private static IEnumerator OnUiManagerInitCoro(Action code) {");
            generatedCode.AppendLine("    while (GetUiManager() == null)");
            generatedCode.AppendLine("        yield return null;");
            generatedCode.AppendLine("    code();");
            generatedCode.AppendLine("}");
            
            generatedCode.AppendLine("internal static void CheckA() {");
            generatedCode.AppendLine("    try {");
            generatedCode.AppendLine("        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(\"_dummy_.dll\");");
            generatedCode.AppendLine("        using var memStream = new MemoryStream((int) stream.Length);");
            generatedCode.AppendLine("        stream.CopyTo(memStream);");
            generatedCode.AppendLine("        Assembly.Load(memStream.ToArray());");
            PrintCheckFailedCode(generatedCode, 2);
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("    catch (BadImageFormatException) {}");
            generatedCode.AppendLine("}");
            
            generatedCode.AppendLine("internal static void CheckB() {");
            generatedCode.AppendLine("    try {");
            generatedCode.AppendLine("        using var stream1 = Assembly.GetExecutingAssembly().GetManifestResourceStream(\"_dummy2_.dll\");");
            generatedCode.AppendLine("        using var memStream1 = new MemoryStream((int) stream1.Length);");
            generatedCode.AppendLine("        stream1.CopyTo(memStream1);");
            generatedCode.AppendLine("        Assembly.Load(memStream1.ToArray());");
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("    catch (BadImageFormatException) {");
            PrintCheckFailedCode(generatedCode, 2);
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("}");

            generatedCode.AppendLine("internal static void CheckC() {");
            generatedCode.AppendLine("    try {");
            generatedCode.AppendLine("        var harmony = new HarmonyLib.Harmony(Guid.NewGuid().ToString());");
            generatedCode.AppendLine($"        harmony.Patch(AccessTools.Method(typeof({modTypeName}), nameof(PatchTest)),");
            generatedCode.AppendLine($"            new HarmonyMethod(typeof({modTypeName}), nameof(ReturnFalse)));");
            generatedCode.AppendLine("        PatchTest();");
            PrintCheckFailedCode(generatedCode, 2);
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("    catch (BadImageFormatException) {}");
            generatedCode.AppendLine("}");

            generatedCode.AppendLine("private static bool ReturnFalse() => false;");
            generatedCode.AppendLine("private static void PatchTest() => throw new BadImageFormatException();");
            generatedCode.AppendLine("private static void PatchTast() => throw new BadImageFormatException();");
            
            generatedCode.AppendLine("internal static void CheckDummyThree() {");
            generatedCode.AppendLine("    try {");
            generatedCode.AppendLine("        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(\"_dummy3_.dll\");");
            generatedCode.AppendLine("        using var memStream = new MemoryStream((int) stream.Length);");
            generatedCode.AppendLine("        stream.CopyTo(memStream);");
            generatedCode.AppendLine("        Assembly.Load(memStream.ToArray()).GetTypes();");
            generatedCode.AppendLine("        while(true);");
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("    catch (BadImageFormatException)");
            generatedCode.AppendLine("    {");
            generatedCode.AppendLine("    }");
            generatedCode.AppendLine("}");
            
            generatedCode.AppendLine("private static readonly string[] ourAnnoyingMessages = {");
            generatedCode.AppendLine("    \"===================================================================\",");
            generatedCode.AppendLine("    \"I'm afraid I can't let you do that, Dave\",");
            generatedCode.AppendLine("    \"\",");
            generatedCode.AppendLine("    \"You're using MelonLoader with important security features missing.\",");
            generatedCode.AppendLine("    \"In addition to such versions being a requirement for malicious mods,\",");
            generatedCode.AppendLine("    \"this exposes you to additional risks from certain malicious actors,\",");
            generatedCode.AppendLine("    \"including ACCOUNT THEFT, ACCOUNT BANS, and other unwanted consequences\",");
            generatedCode.AppendLine("    \"This is not limited to VRChat - other accounts (i.e. Discord) can be affected\",");
            generatedCode.AppendLine("    \"This is not what you want, so download the official installer from\",");
            generatedCode.AppendLine("    \"https://github.com/LavaGang/MelonLoader/releases\",");
            generatedCode.AppendLine("    \"then close this console, and reinstall MelonLoader using it.\",");
            generatedCode.AppendLine("    \"\",");
            generatedCode.AppendLine("    \"You can read more about why this message is a thing here:\",");
            generatedCode.AppendLine("    \"https://github.com/knah/VRCMods/blob/master/Malicious-Mods.md\",");
            generatedCode.AppendLine("    \"\",");
            generatedCode.AppendLine("    \"Rejecting malicious mods is the only way forward.\",");
            generatedCode.AppendLine("    \"Pressing enter will close VRChat.\",");
            generatedCode.AppendLine("    \"===================================================================\",");
            generatedCode.AppendLine("};");

            generatedCode.AppendLine("}");
            generatedCode.AppendLine("}");
        
            context.AddSource($"{modTypeName}.Generated.cs", generatedCode.ToString());
        }

        private static void PrintCheckFailedCode(StringBuilder builder, int indent)
        {
            var prefix = "".PadLeft(indent * 4, ' ');
            builder.AppendLine(prefix + "try {");
            builder.AppendLine(prefix + "    MustStayFalse = true;");
            builder.AppendLine(prefix + "    foreach (var message in ourAnnoyingMessages) MelonLogger.Error(message);");
            builder.AppendLine(prefix + "    Console.In.ReadLine();");
            builder.AppendLine(prefix + "    Environment.Exit(1);");
            builder.AppendLine(prefix + "} finally {");
            builder.AppendLine(prefix + "    try { Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))(); } finally { while(true); }");
            builder.AppendLine(prefix + "}");
        }
    }
}