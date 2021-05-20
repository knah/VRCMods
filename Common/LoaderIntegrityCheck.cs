using System;
using System.IO;
using System.Reflection;
using Harmony;
using MelonLoader;


[HarmonyShield]
internal static class LoaderIntegrityCheck
{
    private static string FilteredAssemblyName
    {
        get
        {
            var simpleName = Assembly.GetExecutingAssembly().GetName().Name;
            var minusIndex = simpleName.IndexOf('-');
            if (minusIndex >= 0)
                return simpleName.Substring(0, minusIndex);
            return simpleName;
        }
    }
    
    public static void CheckIntegrity()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(FilteredAssemblyName + ".Common._dummy_.dll");
            using var memStream = new MemoryStream((int) stream.Length);
            stream.CopyTo(memStream);

            Assembly.Load(memStream.ToArray());

            PrintWarningMessage();

            while (Console.In.Peek() != '\n') Console.In.Read();
        }
        catch (BadImageFormatException)
        {
        }

        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(FilteredAssemblyName + ".Common._dummy2_.dll");
            using var memStream = new MemoryStream((int) stream.Length);
            stream.CopyTo(memStream);

            Assembly.Load(memStream.ToArray());
        }
        catch (BadImageFormatException ex)
        {
            MelonLogger.Error(ex.ToString());

            PrintWarningMessage();

            while (Console.In.Peek() != '\n') Console.In.Read();
        }

        try
        {
            var harmony = HarmonyInstance.Create(Guid.NewGuid().ToString());
            harmony.Patch(AccessTools.Method(typeof(LoaderIntegrityCheck), nameof(PatchTest)),
                new HarmonyMethod(typeof(LoaderIntegrityCheck), nameof(ReturnFalse)));

            PatchTest();

            PrintWarningMessage();

            while (Console.In.Peek() != '\n') Console.In.Read();
        }
        catch (BadImageFormatException)
        {
        }
    }

    private static bool ReturnFalse() => false;

    public static void PatchTest()
    {
        throw new BadImageFormatException();
    }

    private static void PrintWarningMessage()
    {
        MelonLogger.Error("===================================================================");
        MelonLogger.Error("You're using MelonLoader with important security features missing.");
        MelonLogger.Error("This exposes you to additional risks from certain malicious actors,");
        MelonLogger.Error("including account theft, account bans, and other unwanted consequences");
        MelonLogger.Error("If this is not what you want, download the official installer from");
        MelonLogger.Error("https://github.com/LavaGang/MelonLoader/releases");
        MelonLogger.Error("then close this console, and reinstall MelonLoader using it.");
        MelonLogger.Error("If you want to accept those risks, press Enter to continue");
        MelonLogger.Error("===================================================================");
    }
}