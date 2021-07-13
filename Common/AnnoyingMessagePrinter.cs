
using System.Reflection;
using MelonLoader;

internal static class AnnoyingMessagePrinter
{
    internal static void PrintWarningMessage()
    {
        MelonLogger.Error("===================================================================");
        MelonLogger.Error("I'm afraid I can't let you do that, Dave");
        MelonLogger.Error("");
        MelonLogger.Error("You're using MelonLoader with important security features missing.");
        MelonLogger.Error("In addition to such versions being a requirement for malicious mods,");
        MelonLogger.Error("this exposes you to additional risks from certain malicious actors,");
        MelonLogger.Error("including ACCOUNT THEFT, ACCOUNT BANS, and other unwanted consequences");
        MelonLogger.Error("This is not limited to VRChat - other accounts (i.e. Discord) can be affected");
        MelonLogger.Error("This is not what you want, so download the official installer from");
        MelonLogger.Error("https://github.com/LavaGang/MelonLoader/releases");
        MelonLogger.Error("then close this console, and reinstall MelonLoader using it.");
        MelonLogger.Error("");
        MelonLogger.Error("You can read more about why this message is a thing here:");
        MelonLogger.Error("https://github.com/knah/VRCMods/blob/master/Malicious-Mods.md");
        MelonLogger.Error("");
        MelonLogger.Error("Rejecting malicious mods is the only way forward.");
        MelonLogger.Error("Pressing enter will close VRChat.");
        MelonLogger.Error("===================================================================");
    }
}
