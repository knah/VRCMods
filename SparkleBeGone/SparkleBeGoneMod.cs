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

[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(SparkleBeGoneMod), "SparkleBeGone", "1.1.0", "knah", "https://github.com/knah/VRCMods")]

namespace SparkleBeGone
{
    public class SparkleBeGoneMod : CustomizedMelonMod
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidDelegate(IntPtr thisPtr);

        private Texture2D myWhiteLaserTexture;
        private Texture2D myOriginalLaserTexture;

        private static VoidDelegate ourOriginalLateUpdate;
        private static Color ourBeamColor;
        private static bool ourDoRecolorSparks;
        private static bool ourDoRecolorBeams;
        
        private static VRCSpaceUiPointer ourLeftPointer;
        private static VRCSpaceUiPointer ourRightPointer;
        private static readonly int ourTintColor = Shader.PropertyToID("_TintColor");

        private static MelonPreferences_Entry<bool> ourStartSparkle;
        private static MelonPreferences_Entry<bool> ourEndSparkle;
        private static MelonPreferences_Entry<bool> ourEndFlare;

        public override void OnApplicationStart()
        {
            var category = MelonPreferences.CreateCategory("SparkleBeGone", "Sparkle Be Gone");
            
            ourStartSparkle = category.CreateEntry("StartSparkle", false, "Show start sparkle");
            ourEndSparkle = category.CreateEntry("EndSparks", false, "Show end sparks");
            ourEndFlare = category.CreateEntry("EndFlare", true, "Show end flare");
            
            var recolorSparks = category.CreateEntry("RecolorSparks", false, "Recolor sparks");
            var recolorBeams = category.CreateEntry("RecolorBeams", true, "Recolor beams");
            var beamColor = category.CreateEntry("BeamColor", "25 50 255 255", "Beam color (r g b a)");
            
            recolorSparks.OnValueChanged += (_, value) => ourDoRecolorSparks = value;
            recolorBeams.OnValueChanged += (_, value) =>
            {
                ourDoRecolorBeams = value;
                UpdateBeamTextures();
            };
            beamColor.OnValueChanged += (_, value) => ourBeamColor = DecodeColor(value);

            ourDoRecolorSparks = recolorSparks.Value;
            ourDoRecolorBeams = recolorBeams.Value;
            ourBeamColor = DecodeColor(beamColor.Value);

            ourStartSparkle.OnValueChanged += (_, _) => UpdateParticleSystems();
            ourEndSparkle.OnValueChanged += (_, _) => UpdateParticleSystems();
            ourEndFlare.OnValueChanged += (_, _) => UpdateParticleSystems();

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
                var method = typeof(VRCSpaceUiPointer).GetMethod(nameof(VRCSpaceUiPointer.LateUpdate), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var originalPointer = *(IntPtr*)(IntPtr) UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method).GetValue(null);
                MelonUtils.NativeHookAttach((IntPtr) (&originalPointer), typeof(SparkleBeGoneMod).GetMethod(nameof(CursorLateUpdatePatch), BindingFlags.Static | BindingFlags.NonPublic)!.MethodHandle.GetFunctionPointer());
                ourOriginalLateUpdate = Marshal.GetDelegateForFunctionPointer<VoidDelegate>(originalPointer);
            }

            MelonCoroutines.Start(InitThings());
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

        private void UpdateBeamTextures()
        {
            if (ourLeftPointer == null) return;
            
            if (myOriginalLaserTexture == null)
            {
                myOriginalLaserTexture = ourLeftPointer.GetComponent<LineRenderer>().material.mainTexture.Cast<Texture2D>();
                myOriginalLaserTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            var targetTexture = ourDoRecolorBeams ? myWhiteLaserTexture : myOriginalLaserTexture;
            ourLeftPointer.GetComponent<LineRenderer>().material.mainTexture = targetTexture;
            ourRightPointer.GetComponent<LineRenderer>().material.mainTexture = targetTexture;
        }

        public void UpdateParticleSystems()
        {
            if (ourLeftPointer == null) return;
            
            AdjustParticleSystems(ourLeftPointer.gameObject);
            AdjustParticleSystems(ourRightPointer.gameObject);
        }

        private static void CursorLateUpdatePatch(IntPtr thisPtr)
        {
            try
            {
                ourOriginalLateUpdate(thisPtr);
                
                if (ourLeftPointer == null) return;

                VRCSpaceUiPointer pointer = null;
                if (thisPtr == ourLeftPointer.Pointer)
                    pointer = ourLeftPointer;
                else if (thisPtr == ourRightPointer.Pointer)
                    pointer = ourRightPointer;

                if (pointer == null) return;

                if (ourDoRecolorSparks) // this affects particles
                {
                    var materials = pointer.field_Private_ArrayOf_Material_0;
                    if (materials != null)
                        foreach (var material in materials)
                            material.SetColor(ourTintColor, ourBeamColor);
                }

                if (!ourDoRecolorBeams) return;
                
                var lineRenderer = pointer.field_Public_LineRenderer_0;
                if (lineRenderer != null)
                    lineRenderer.startColor = lineRenderer.endColor = ourBeamColor;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(ex.ToString());
            }
        }

        private IEnumerator InitThings()
        {
            while (VRCUiCursorManager.field_Private_Static_VRCUiCursorManager_0 == null) yield return null;
            
            var cursorManager = VRCUiCursorManager.field_Private_Static_VRCUiCursorManager_0;

            ourLeftPointer = cursorManager.transform.Find("LeftHandBeam").GetComponent<VRCSpaceUiPointer>();
            ourRightPointer = cursorManager.transform.Find("RightHandBeam").GetComponent<VRCSpaceUiPointer>();
            
            UpdateBeamTextures();
            UpdateParticleSystems();
        }

        private void AdjustParticleSystems(GameObject cursorRoot)
        {
            var startParticle = cursorRoot.transform.Find("plasma_beam_muzzle_blue");
            var endFlare = cursorRoot.transform.Find("plasma_beam_flare_blue");
            var endSparks = endFlare.Find("plasma_beam_spark_002");
            
            startParticle.GetComponent<ParticleSystem>().enableEmission = ourStartSparkle.Value; 
            endFlare.GetComponent<ParticleSystem>().enableEmission = ourEndFlare.Value; 
            endSparks.GetComponent<ParticleSystem>().enableEmission = ourEndSparkle.Value;
        }
    }
}