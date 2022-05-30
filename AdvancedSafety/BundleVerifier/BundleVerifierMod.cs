using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using MelonLoader;
using VRC.Core;

namespace AdvancedSafety.BundleVerifier
{
    public static class BundleVerifierMod
    {
        internal static MelonPreferences_Entry<int> TimeLimit;
        internal static MelonPreferences_Entry<int> MemoryLimit;
        internal static MelonPreferences_Entry<int> ComponentLimit;
        
        internal static MelonPreferences_Entry<bool> OnlyPublics;
        internal static MelonPreferences_Entry<bool> EnabledSetting;

        internal static BundleHashCache BadBundleCache;
        internal static BundleHashCache ForceAllowedCache;

        internal static string BundleVerifierPath;

        public static void OnApplicationStart(HarmonyLib.Harmony harmonyInstance)
        {
            var category = MelonPreferences.CreateCategory(SettingsCategory, "Advanced Safety - Bundles");
            
            TimeLimit = category.CreateEntry("TimeLimit", 15, "Time limit (seconds)");
            MemoryLimit = category.CreateEntry("MemLimit", 2048, "Memory limit (megabytes)");
            ComponentLimit = category.CreateEntry("ComponentLimit", 10_000, "Component limit (0=unlimited)");

            EnabledSetting = category.CreateEntry("Enabled", true, "Check for corrupted bundles");
            OnlyPublics = category.CreateEntry("OnlyPublics", true, "Only check bundles in public worlds");

            BadBundleCache = new BundleHashCache(Path.Combine(MelonUtils.UserDataDirectory, "BadBundleHashes.bin"));
            ForceAllowedCache = new BundleHashCache(null);

            var initSuccess = BundleDownloadMethods.Init();
            if (!initSuccess) return;

            try
            {
                PrepareVerifierDir();
            }
            catch (IOException ex)
            {
                MelonLogger.Error("Unable to extract bundle verifier app, the mod will not work");
                MelonLogger.Error(ex.ToString());
                return;
            }

            harmonyInstance.Patch(typeof(NetworkManager).GetMethod("OnLeftRoom"), new HarmonyMethod(typeof(BundleVerifierMod), nameof(OnLeftRoom)));
            harmonyInstance.Patch(typeof(NetworkManager).GetMethod("OnJoinedRoom"), new HarmonyMethod(typeof(BundleVerifierMod), nameof(OnJoinedRoom)));
            
            EnabledSetting.OnValueChangedUntyped += () => MelonCoroutines.Start(CheckInstanceType());
            OnlyPublics.OnValueChangedUntyped += () => MelonCoroutines.Start(CheckInstanceType());
        }

        public static void OnApplicationQuit()
        {
            BadBundleCache?.Dispose();
        }

        private static void OnLeftRoom()
        {
            MelonDebug.Msg("Left room, disabling intercept");
            BundleDlInterceptor.ShouldIntercept = false;
        }
        
        private static void OnJoinedRoom()
        {
            MelonDebug.Msg("Joined room, starting instance check");
            MelonCoroutines.Start(CheckInstanceType());
        }

        private static IEnumerator CheckInstanceType()
        {
            while (RoomManager.field_Internal_Static_ApiWorldInstance_0 == null)
                yield return null;

            if (!EnabledSetting.Value)
            {
                BundleDlInterceptor.ShouldIntercept = false;
                MelonLogger.Msg($"Bundle intercept disabled in settings");
                yield break;
            }

            var currentInstance = RoomManager.field_Internal_Static_ApiWorldInstance_0;
            BundleDlInterceptor.ShouldIntercept = !OnlyPublics.Value || currentInstance.type == InstanceAccessType.Public;
            MelonDebug.Msg($"Got instance, intercept state: {BundleDlInterceptor.ShouldIntercept}");
        }

        private const string VerifierVersion = "1.4-2019.4.31";
        internal const string SettingsCategory = "ASBundleVerifier";

        private static void PrepareVerifierDir()
        {
            var baseDir = Path.Combine(MelonUtils.UserDataDirectory, "BundleVerifier");
            Directory.CreateDirectory(baseDir);
            BundleVerifierPath = Path.Combine(baseDir, "BundleVerifier.exe");
            var versionFile = Path.Combine(baseDir, "version.txt");
            if (File.Exists(versionFile))
            {
                var existingVersion = File.ReadAllText(versionFile);
                if (existingVersion == VerifierVersion) return;
            }
            
            BadBundleCache.Clear();
            
            File.Copy(Path.Combine(MelonUtils.GameDirectory, "UnityPlayer.dll"), Path.Combine(baseDir, "UnityPlayer.dll"), true);
            using var zipFile = new ZipArchive(Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedSafety.BundleVerifier.BundleVerifier.zip")!, ZipArchiveMode.Read, false);
            foreach (var zipArchiveEntry in zipFile.Entries)
            {
                var targetFile = Path.Combine(baseDir, zipArchiveEntry.FullName);
                var looksLikeDir = Path.GetFileName(targetFile).Length == 0;
                Directory.CreateDirectory(looksLikeDir
                    ? targetFile
                    : Path.GetDirectoryName(targetFile)!);
                if (!looksLikeDir)
                    zipArchiveEntry.ExtractToFile(targetFile, true);
            }
            
            File.WriteAllText(versionFile, VerifierVersion);
        }

        private static readonly Regex ourUrlRegex = new("file_([^/]+)/([^/]+)");
        internal static (string, string) SanitizeUrl(string url)
        {
            var matches = ourUrlRegex.Match(url);
            if (!matches.Success) return ("", url);

            var chars = matches.Groups[1].Value.ToCharArray();
            Array.Reverse(chars);
            
            return (new string(chars), matches.Groups[2].Value);
        }
    }
}