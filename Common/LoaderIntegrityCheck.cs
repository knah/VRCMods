using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;


[PatchShield]
internal static class LoaderIntegrityCheck
{
    public static void CheckIntegrity()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("_dummy_.dll");
            using var memStream = new MemoryStream((int) stream.Length);
            stream.CopyTo(memStream);

            Assembly.Load(memStream.ToArray());

            AnnoyingMessagePrinter.PrintWarningMessage();

            Console.In.ReadLine();
            Environment.Exit(1);
            Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))();
        }
        catch (BadImageFormatException)
        {
        }

        try
        {
            using var stream1 = Assembly.GetExecutingAssembly().GetManifestResourceStream("_dummy2_.dll");
            using var memStream1 = new MemoryStream((int) stream1.Length);
            stream1.CopyTo(memStream1);

            Assembly.Load(memStream1.ToArray());
        }
        catch (BadImageFormatException ex)
        {
            MelonLogger.Error(ex.ToString());

            AnnoyingMessagePrinter.PrintWarningMessage();

            Console.In.ReadLine();
            Environment.Exit(1);
            Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))();
        }
        
        CheckC();
    }

    internal static void CheckC()
    {
        try
        {
            var harmony = new HarmonyLib.Harmony(Guid.NewGuid().ToString());
            harmony.Patch(typeof(LoaderIntegrityCheck).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(it => it.ReturnType == typeof(int)),
                new HarmonyMethod(typeof(LoaderIntegrityCheck).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(it => it.ReturnType == typeof(bool))));

            PatchTest();

            AnnoyingMessagePrinter.PrintWarningMessage();
            
            Console.In.ReadLine();
            Environment.Exit(1);
            Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))();
        }
        catch (BadImageFormatException)
        {
        }
    }
    
    internal static void CheckDummyThree()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("_dummy3_.dll");
            using var memStream = new MemoryStream((int) stream.Length);
            stream.CopyTo(memStream);

            Assembly.Load(memStream.ToArray()).GetTypes();

            AnnoyingMessagePrinter.PrintWarningMessage();

            Environment.Exit(1);
            Marshal.GetDelegateForFunctionPointer<Action>(Marshal.AllocHGlobal(16))();
        }
        catch (BadImageFormatException)
        {
        }
    } 

    private static bool ReturnFalse() => false;

    private static int PatchTest()
    {
        throw new BadImageFormatException();
    }
}