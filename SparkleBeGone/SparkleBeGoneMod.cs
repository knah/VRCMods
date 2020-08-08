using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using SparkleBeGone;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;

[assembly:MelonModGame("VRChat", "VRChat")]
[assembly:MelonModInfo(typeof(SparkleBeGoneMod), "SparkleBeGone", "1.0.1", "knah", "https://github.com/knah/VRCMods")]

namespace SparkleBeGone
{
    public class SparkleBeGoneMod : MelonMod
    {
        private const string SettingsCategory = "SparkleBeGone";
        
        private const string StartSparkleSetting = "StartSparkle";
        private const string EndSparksSetting = "EndSparks";
        private const string EndFlareSetting = "EndFlare";
        
        private const string RecolorSparksSetting = "RecolorSparks";
        private const string RecolorBeamsSetting = "RecolorBeams";

        private const string BeamColorSetting = "BeamColor";
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidDelegate(IntPtr thisPtr);

        private Texture2D myWhiteLaserTexture;
        private Texture2D myOriginalLaserTexture;

        private static VoidDelegate ourOriginalLateUpdate;
        private static Color ourBeamColor;
        private static bool ourDoRecolorSparks;
        private static bool ourDoRecolorBeams;
        
        public override void OnApplicationStart()
        {
            MelonPrefs.RegisterCategory(SettingsCategory, "Sparkle Be Gone");
            
            MelonPrefs.RegisterBool(SettingsCategory, StartSparkleSetting, false, "Show start sparkle");
            MelonPrefs.RegisterBool(SettingsCategory, EndSparksSetting, false, "Show end sparks");
            MelonPrefs.RegisterBool(SettingsCategory, EndFlareSetting, true, "Show end flare");
            
            MelonPrefs.RegisterBool(SettingsCategory, RecolorSparksSetting, false, "Recolor sparks");
            MelonPrefs.RegisterBool(SettingsCategory, RecolorBeamsSetting, true, "Recolor beams");
            
            MelonPrefs.RegisterString(SettingsCategory, BeamColorSetting, "25 50 255 255", "Beam color (r g b a)");

            var method = typeof(VRCSpaceUiPointer).GetMethod(nameof(VRCSpaceUiPointer.LateUpdate), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SparkleBeGone.sparklebegone");
                using var memStream = new MemoryStream((int) stream.Length);
                stream.CopyTo(memStream);
                var bundle = AssetBundle.LoadFromMemory_Internal(memStream.ToArray(), 0);
                myWhiteLaserTexture = bundle.LoadAsset_Internal("Assets/SparkleBeGone/sniper_beam_white.png", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
                myWhiteLaserTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            unsafe
            {
                var originalPointer = *(IntPtr*)(IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method).GetValue(null);
                CompatHook((IntPtr) (&originalPointer), typeof(SparkleBeGoneMod).GetMethod(nameof(CursorLateUpdatePatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
                ourOriginalLateUpdate = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(originalPointer);
            }

            MelonCoroutines.Start(InitThings());
        }
        
        private static void CompatHook(IntPtr first, IntPtr second)
        {
            typeof(Imports).GetMethod("Hook", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!
                .Invoke(null, new object[] {first, second});
        }

        private Color DecodeColor(string color)
        {
            var split = color.Split(' ');
            int red = 255;
            int green = 255;
            int blue = 255;
            int alpha = 255;

            if (split.Length > 0) int.TryParse(split[0], out red);
            if (split.Length > 1) int.TryParse(split[1], out green);
            if (split.Length > 2) int.TryParse(split[2], out blue);
            if (split.Length > 3) int.TryParse(split[3], out alpha);
            
            return new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
        }

        public override void OnModSettingsApplied()
        {
            var cursorManager = VRCUiCursorManager.field_Private_Static_VRCUiCursorManager_0;

            if (cursorManager == null) return;

            var leftHand = cursorManager.handLeftCursor;
            var rightHand = cursorManager.handRightCursor;

            if (myOriginalLaserTexture == null)
            {
                myOriginalLaserTexture = leftHand.GetComponent<LineRenderer>().material.mainTexture.Cast<Texture2D>();
                myOriginalLaserTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            var targetTexture = MelonPrefs.GetBool(SettingsCategory, RecolorBeamsSetting) ? myWhiteLaserTexture : myOriginalLaserTexture;
            leftHand.GetComponent<LineRenderer>().material.mainTexture = targetTexture;
            rightHand.GetComponent<LineRenderer>().material.mainTexture = targetTexture;

            AdjustParticleSystems(leftHand.gameObject);
            AdjustParticleSystems(rightHand.gameObject);

            var color = DecodeColor(MelonPrefs.GetString(SettingsCategory, BeamColorSetting));
            ourBeamColor = color;
            ourDoRecolorSparks = MelonPrefs.GetBool(SettingsCategory, RecolorSparksSetting);
            ourDoRecolorBeams = MelonPrefs.GetBool(SettingsCategory, RecolorBeamsSetting);
        }

        private static void CursorLateUpdatePatch(IntPtr thisPtr)
        {
            try
            {
                var pointer = new VRCSpaceUiPointer(thisPtr);

                ourOriginalLateUpdate(thisPtr);

                if (ourDoRecolorSparks) // this affects particles
                {
                    var materials = pointer.field_Private_ArrayOf_Material_0;
                    if (materials != null)
                        foreach (var material in materials)
                            material.SetColor("_TintColor", ourBeamColor);
                }

                if (!ourDoRecolorBeams) return;
                
                var lineRenderer = pointer.lineRenderer;
                if (lineRenderer != null)
                    lineRenderer.startColor = lineRenderer.endColor = ourBeamColor;
            }
            catch (Exception ex)
            {
                MelonModLogger.LogError(ex.ToString());
            }
        }

        private IEnumerator InitThings()
        {
            while (VRCUiCursorManager.field_Private_Static_VRCUiCursorManager_0 == null) yield return null;
            
            OnModSettingsApplied();
        }

        private void AdjustParticleSystems(GameObject cursorRoot)
        {
            var startParticle = cursorRoot.transform.Find("plasma_beam_muzzle_blue");
            var endFlare = cursorRoot.transform.Find("plasma_beam_flare_blue");
            var endSparks = endFlare.Find("plasma_beam_spark_002");
            
            startParticle.GetComponent<ParticleSystem>().enableEmission = MelonPrefs.GetBool(SettingsCategory, StartSparkleSetting); 
            endFlare.GetComponent<ParticleSystem>().enableEmission = MelonPrefs.GetBool(SettingsCategory, EndFlareSetting); 
            endSparks.GetComponent<ParticleSystem>().enableEmission = MelonPrefs.GetBool(SettingsCategory, EndSparksSetting);
        }
    }
}