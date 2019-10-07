using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRCModLoader;
using VRCTools;
using VRCTools.utils;

namespace JoinNotifier
{
    [VRCModInfo("JoinNotifier", VersionConst, "knah")]
    public class JoinNotifierMod : VRCMod
    {
        public const string VersionConst = "0.1.1";

        private Image myNotifierImage;
        private AudioSource myChimeSource;
        private int myLastLevelLoad;

        [UsedImplicitly]
        public void OnApplicationStart()
        {
            ModManager.StartCoroutine(InitThings());
        }

        public IEnumerator InitThings()
        {
            JoinNotifierSettings.RegisterSettings();
            yield return NetworkManagerHooks.WaitForNmInit();
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            yield return VrcAudioManagerReflection.WaitForAudioManager();
            CreateNotifierImage();
            NetworkManagerHooks.AddPlayerJoinHook(OnPlayerJoined);
        }

        [UsedImplicitly]
        public void OnModSettingsApplied()
        {
            if (myChimeSource != null)
                myChimeSource.volume = JoinNotifierSettings.GetSoundVolume();
        }

        public void CreateNotifierImage()
        {
//            var pathToThing = "UserInterface/UnscaledUI/HudContent/Hud/NotificationDotParent/NotificationDot";
            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            var indicator = GameObject.Instantiate(hudRoot.transform.Find("NotificationDotParent/NotificationDot").gameObject, hudRoot.transform.Find("NotificationDotParent"));
            indicator.name = "PlayerJoinedDot";
            var image = indicator.GetComponent<Image>();
            var texture = new Texture2D(2, 2);
            using (var resourceStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("JoinNotifier.JoinIcon.png"))
            {
                var bytes = new byte[resourceStream.Length];
                resourceStream.Read(bytes, 0, (int) resourceStream.Length);
                Texture2DUtils.LoadImage(texture, bytes); // todo: actual png texture
            }

            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width / 2f, texture.height / 2f), 100f);
            myNotifierImage = image;
            image.enabled = false;

            using (var resourceStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("JoinNotifier.Chime.bin"))
            { // chime.bin is an array of 16 bit signed integers; obtainable by exporting from audacity as header-less PCM
                var source = indicator.AddComponent<AudioSource>();
                var numSamples = (int) resourceStream.Length / 2;
                var clip = AudioClip.Create("chime.bin", numSamples, 1, 44100, false);
                source.clip = clip;
                float[] data = new float[numSamples];
                using (var reader = new BinaryReader(resourceStream))
                {
                    for (var i = 0; i < numSamples; i++) 
                        data[i] = reader.ReadInt16() / (float) Int16.MaxValue;
                }

                clip.SetData(data, 0);
                source.spatialize = false;
                source.volume = JoinNotifierSettings.GetSoundVolume();
                source.loop = false;
                source.playOnAwake = false;
                source.outputAudioMixerGroup = VrcAudioManagerReflection.GetAudioManager().uiGroup;
                myChimeSource = source;
            }
            
            indicator.SetActive(true);
        }

        [UsedImplicitly]
        public void OnLevelWasInitialized(int level)
        {
            myLastLevelLoad = Environment.TickCount;
        }

        public void OnPlayerJoined(Player player)
        {
            if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
            if (Environment.TickCount - myLastLevelLoad < 5_000) return;
            VRCModLogger.Log("[JoinNotifier] Notifying player join");
            if (JoinNotifierSettings.ShouldBlinkIcon())
                ModManager.StartCoroutine(BlinkIconCoroutine());
            if (JoinNotifierSettings.ShouldPlaySound())
               myChimeSource.Play(); 
        }

        public IEnumerator BlinkIconCoroutine()
        {
            for (var i = 0; i < 3; i++)
            {
                myNotifierImage.enabled = true;
                yield return new WaitForSeconds(.5f);
                myNotifierImage.enabled = false;
                yield return new WaitForSeconds(.5f);
            }
        }
    }
}