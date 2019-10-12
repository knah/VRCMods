using System;
using System.Collections;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRCModLoader;
using VRCTools;
using VRCTools.utils;
using Object = UnityEngine.Object;

namespace JoinNotifier
{
    [VRCModInfo("JoinNotifier", VersionConst, "knah")]
    public class JoinNotifierMod : VRCMod
    {
        public const string VersionConst = "0.2.0";

        private Image myJoinImage;
        private Image myLeaveImage;
        private AudioSource myJoinSource;
        private AudioSource myLeaveSource;
        private Text myJoinText;
        private Text myLeaveText;
        
        private int myLastLevelLoad;
        private bool myObservedLocalPlayerJoin;

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
            CreateGameObjects();
            NetworkManagerHooks.AddPlayerJoinHook(OnPlayerJoined);
            NetworkManagerHooks.AddPlayerLeftHook(OnPlayerLeft);
            VRCModLogger.Log("[JoinNotifier] Init done");
        }

        [UsedImplicitly]
        public void OnModSettingsApplied()
        {
            if (myJoinSource != null)
            {
                myJoinSource.volume = JoinNotifierSettings.GetSoundVolume();
                myJoinSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? VrcAudioManagerReflection.GetAudioManager().uiGroup : null;
            }

            if (myLeaveSource != null)
            {
                myLeaveSource.volume = JoinNotifierSettings.GetSoundVolume();
                myLeaveSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? VrcAudioManagerReflection.GetAudioManager().uiGroup : null;
            }

            if (myJoinImage != null)
                myJoinImage.color = JoinNotifierSettings.GetJoinIconColor();
            
            if (myLeaveImage != null)
                myLeaveImage.color = JoinNotifierSettings.GetLeaveIconColor();
            
            if (myJoinText != null)
            {
                myJoinText.fontSize = JoinNotifierSettings.GetTextSize();
                myJoinText.color = JoinNotifierSettings.GetJoinIconColor();
            }

            if (myLeaveText != null)
            {
                myLeaveText.fontSize = JoinNotifierSettings.GetTextSize();
                myLeaveText.color = JoinNotifierSettings.GetLeaveIconColor();
            }

        }

        private Image CreateNotifierImage(string name, float offset, Color colorTint)
        {
            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            var indicator = Object.Instantiate(hudRoot.transform.Find("NotificationDotParent/NotificationDot").gameObject, hudRoot.transform.Find("NotificationDotParent"));
            indicator.name = "NotifyDot-" + name;
            indicator.SetActive(true);
            indicator.transform.localPosition += Vector3.right * offset;
            var image = indicator.GetComponent<Image>();
            var texture = new Texture2D(2, 2);
            using (var resourceStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("JoinNotifier.JoinIcon.png"))
            {
                var bytes = new byte[resourceStream.Length];
                resourceStream.Read(bytes, 0, (int) resourceStream.Length);
                Texture2DUtils.LoadImage(texture, bytes);
            }

            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width / 2f, texture.height / 2f), 100f);
            image.enabled = false;
            image.color = colorTint;

            return image;
        }

        private Text CreateTextNear(Image image, float offset, TextAnchor alignment)
        {
            var gameObject = new GameObject(image.gameObject.name + "-text", typeof(Text));
            gameObject.transform.SetParent(image.transform, false);
            gameObject.transform.localScale = Vector3.one;
            gameObject.transform.localPosition = Vector3.up * offset;
            var text = gameObject.GetComponent<Text>();
            text.color = image.color;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = alignment;
            text.fontSize = JoinNotifierSettings.GetTextSize();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            gameObject.SetActive(true);
            return text;
        }

        private AudioSource CreateAudioSource(string resourceName, GameObject parent)
        {
            using (var resourceStream =
                Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            { // chime.bin is an array of 16 bit signed integers; obtainable by exporting from audacity as header-less PCM
                var source = parent.AddComponent<AudioSource>();
                var numSamples = (int) resourceStream.Length / 2;
                var clip = AudioClip.Create(resourceName, numSamples, 1, 44100, false);
                source.clip = clip;
                float[] data = new float[numSamples];
                using (var reader = new BinaryReader(resourceStream))
                    for (var i = 0; i < numSamples; i++)
                        data[i] = reader.ReadInt16() / (float) Int16.MaxValue;

                clip.SetData(data, 0);
                source.spatialize = false;
                source.volume = JoinNotifierSettings.GetSoundVolume();
                source.loop = false;
                source.playOnAwake = false;
                if (JoinNotifierSettings.GetUseUiMixer())
                    source.outputAudioMixerGroup = VrcAudioManagerReflection.GetAudioManager().uiGroup;
                return source;
            }
        }

        private void CreateGameObjects()
        {
//            var pathToThing = "UserInterface/UnscaledUI/HudContent/Hud/NotificationDotParent/NotificationDot";
            myJoinImage = CreateNotifierImage("join", 0f, JoinNotifierSettings.GetJoinIconColor());
            myJoinSource = CreateAudioSource("JoinNotifier.Chime.bin", myJoinImage.gameObject);
            myJoinText = CreateTextNear(myJoinImage, 110f, TextAnchor.LowerRight);
            
            myLeaveImage = CreateNotifierImage("leave", 100f, JoinNotifierSettings.GetLeaveIconColor());
            myLeaveSource = CreateAudioSource("JoinNotifier.DoorClose.bin", myLeaveImage.gameObject);
            myLeaveText = CreateTextNear(myLeaveImage, 110f, TextAnchor.LowerLeft);
        }

        [UsedImplicitly]
        public void OnLevelWasInitialized(int level)
        {
            myLastLevelLoad = Environment.TickCount;
            myObservedLocalPlayerJoin = false;
        }

        public void OnPlayerJoined(Player player)
        {
            var apiUser = player.GetApiUser();
            if (APIUser.CurrentUser.id == apiUser.id)
            {
                myObservedLocalPlayerJoin = true;
                myLastLevelLoad = Environment.TickCount;
            }
            if (!myObservedLocalPlayerJoin || Environment.TickCount - myLastLevelLoad < 5_000) return;
            if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
            var playerName = apiUser.displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(true))
                ModManager.StartCoroutine(BlinkIconCoroutine(myJoinImage));
            if (JoinNotifierSettings.ShouldPlaySound(true))
               myJoinSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(true))
                ModManager.StartCoroutine(ShowName(myJoinText, playerName));
        }
        
        public void OnPlayerLeft(Player player)
        {
            if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
            if (Environment.TickCount - myLastLevelLoad < 5_000) return;
            var playerName = player.GetApiUser().displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(false))
                ModManager.StartCoroutine(BlinkIconCoroutine(myLeaveImage));
            if (JoinNotifierSettings.ShouldPlaySound(false))
                myLeaveSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(false))
                ModManager.StartCoroutine(ShowName(myLeaveText, playerName));
        }

        public IEnumerator ShowName(Text text, string name)
        {
            var currentText = text.text ?? "";
            currentText = currentText.Length == 0 ? name : currentText + "\n" + name;
            text.text = currentText;
            yield return new WaitForSeconds(3);
            currentText = text.text;
            currentText = currentText.Replace(name, "").Trim('\n');
            text.text = currentText;
        }

        public IEnumerator BlinkIconCoroutine(Image imageToBlink)
        {
            for (var i = 0; i < 3; i++)
            {
                imageToBlink.enabled = true;
                yield return new WaitForSeconds(.5f);
                imageToBlink.enabled = false;
                yield return new WaitForSeconds(.5f);
            }
        }
    }
}