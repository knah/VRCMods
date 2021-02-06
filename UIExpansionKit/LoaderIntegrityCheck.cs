using System;
using System.IO;
using System.Reflection;
using Harmony;
using MelonLoader;

namespace UIExpansionKit
{
    [HarmonyShield]
    public static class LoaderIntegrityCheck
    {
        public static void CheckIntegrity()
        {
            try
            {
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UIExpansionKit._dummy_.dll");
                    using var memStream = new MemoryStream((int) stream.Length);
                    stream.CopyTo(memStream);

                    var assembly = Assembly.Load(memStream.ToArray());
                    
                    MelonLogger.LogError("===================================================================");
                    MelonLogger.LogError("You're using MelonLoader with important security features missing.");
                    MelonLogger.LogError("This exposes you to additional risks from certain malicious actors,");
                    MelonLogger.LogError("including account theft, account bans, and other unwanted consequences");
                    MelonLogger.LogError("If this is not what you want, download the official installer from");
                    MelonLogger.LogError("https://github.com/LavaGang/MelonLoader/releases");
                    MelonLogger.LogError("then close this console, and reinstall MelonLoader using it.");
                    MelonLogger.LogError("If you want to accept those risks, press Enter to continue");
                    MelonLogger.LogError("===================================================================");

                    Console.ReadLine();
                }
            }
            catch (BadImageFormatException ex)
            {
            }
        }
    }
}