using System;
using System.Collections;
using System.IO;
using System.Reflection;
using JoinNotifier;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using Object = UnityEngine.Object;

[assembly:MelonModInfo(typeof(JoinNotifierMod), "JoinNotifier", JoinNotifierMod.VersionConst, "knah")]
[assembly:MelonModGame("VRChat", "VRChat")]

namespace JoinNotifier
{
    public class JoinNotifierMod : MelonMod
    {
        public const string VersionConst = "0.2.4";

        private Image myJoinImage;
        private Image myLeaveImage;
        private AudioSource myJoinSource;
        private AudioSource myLeaveSource;
        private Text myJoinText;
        private Text myLeaveText;
        
        private int myLastLevelLoad;
        private bool myObservedLocalPlayerJoin;
        
        private AssetBundle myAssetBundle;
        private Sprite myJoinSprite;
        private AudioClip myJoinClip;
        private AudioClip myLeaveClip;

        public override void OnApplicationStart()
        {
            MelonModLogger.Log("[JoinNotifier] ApplicationStart");
            JoinNotifierSettings.RegisterSettings();
            MelonModLogger.Log("[JoinNotifier] ApplicationStart done");

            MelonCoroutines.Start(InitThings());
        }

        public IEnumerator InitThings()
        {
            MelonModLogger.Log("[JoinNotifier] Waiting for init");
            
            while (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null)) yield return null;
            while (ReferenceEquals(VRCAudioManager.field_Private_Static_VRCAudioManager_0, null)) yield return null;
            while (ReferenceEquals(VRCUiManager.field_Protected_Static_VRCUiManager_0, null)) yield return null;

            MelonModLogger.Log("[JoinNotifier] Start init");
            
            NetworkManagerHooks.Initialize();

            var tempFile = Path.GetTempFileName();
            using (var tempStream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("JoinNotifier.joinnotifier.assetbundle").CopyTo(tempStream);
            }
            myAssetBundle = AssetBundle.LoadFromFile(tempFile);
            myAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            
            var iconLoadRequest = myAssetBundle.LoadAssetAsync("Assets/JoinNotifier/JoinIcon.png", Il2CppType.Of<Sprite>());
            while (!iconLoadRequest.isDone) yield return null;
            myJoinSprite = iconLoadRequest.asset.Cast<Sprite>();
            myJoinSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            var joinSoundRequest = myAssetBundle.LoadAssetAsync("Assets/JoinNotifier/Chime.ogg", Il2CppType.Of<AudioClip>());
            while (!joinSoundRequest.isDone) yield return null;
            myJoinClip = joinSoundRequest.asset.Cast<AudioClip>();
            myJoinClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            
            var leaveSoundRequest = myAssetBundle.LoadAssetAsync("Assets/JoinNotifier/DoorClose.ogg", Il2CppType.Of<AudioClip>());
            while (!leaveSoundRequest.isDone) yield return null;
            myLeaveClip = leaveSoundRequest.asset.Cast<AudioClip>();
            myLeaveClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            CreateGameObjects();
            
            NetworkManagerHooks.OnJoin += OnPlayerJoined;
            NetworkManagerHooks.OnLeave += OnPlayerLeft;
        }

        public override void OnModSettingsApplied()
        {
            MelonModLogger.Log("[JoinNotifier] Settings apply start");
            if (myJoinSource != null)
            {
                myJoinSource.volume = JoinNotifierSettings.GetSoundVolume();
                myJoinSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? VRCAudioManager.field_Private_Static_VRCAudioManager_0.uiGroup : null;
            }

            if (myLeaveSource != null)
            {
                myLeaveSource.volume = JoinNotifierSettings.GetSoundVolume();
                myLeaveSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? VRCAudioManager.field_Private_Static_VRCAudioManager_0.uiGroup : null;
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
            MelonModLogger.Log("[JoinNotifier] Settings apply done");
        }

        private Image CreateNotifierImage(string name, float offset, Color colorTint)
        {
            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            var requestedParent = hudRoot.transform.Find("NotificationDotParent");
            var indicator = Object.Instantiate(hudRoot.transform.Find("NotificationDotParent/NotificationDot").gameObject, requestedParent, false).Cast<GameObject>();
            indicator.name = "NotifyDot-" + name;
            indicator.SetActive(true);
            indicator.transform.localPosition += Vector3.right * offset;
            var image = indicator.GetComponent<Image>();
            image.sprite = myJoinSprite;

            image.enabled = false;
            image.color = colorTint;

            return image;
        }

        private Text CreateTextNear(Image image, float offset, TextAnchor alignment)
        {
            var gameObject = new GameObject(image.gameObject.name + "-text");
            gameObject.AddComponent<Text>();
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

        private AudioSource CreateAudioSource(AudioClip clip, GameObject parent)
        {
            var source = parent.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialize = false;
            source.volume = JoinNotifierSettings.GetSoundVolume();
            source.loop = false;
            source.playOnAwake = false;
            if (JoinNotifierSettings.GetUseUiMixer())
                source.outputAudioMixerGroup = VRCAudioManager.field_Private_Static_VRCAudioManager_0.uiGroup;
            return source;
        }

        private void CreateGameObjects()
        {
            if (myJoinImage != null) return;

            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            if (hudRoot == null)
            {
                MelonModLogger.Log("[JoinNotifier] Not creating gameobjects - no hud root");
                return;
            }
            
            MelonModLogger.Log("[JoinNotifier] Creating gameobjects");
//            var pathToThing = "UserInterface/UnscaledUI/HudContent/Hud/NotificationDotParent/NotificationDot";
            myJoinImage = CreateNotifierImage("join", 0f, JoinNotifierSettings.GetJoinIconColor());
            myJoinSource = CreateAudioSource(myJoinClip, myJoinImage.gameObject);
            myJoinText = CreateTextNear(myJoinImage, 110f, TextAnchor.LowerRight);
            
            myLeaveImage = CreateNotifierImage("leave", 100f, JoinNotifierSettings.GetLeaveIconColor());
            myLeaveSource = CreateAudioSource(myLeaveClip, myLeaveImage.gameObject);
            myLeaveText = CreateTextNear(myLeaveImage, 110f, TextAnchor.LowerLeft);
        }

        public override void OnLevelWasInitialized(int level)
        {
            MelonModLogger.Log("[JoinNotifier] Scene load");
            
            myLastLevelLoad = Environment.TickCount;
            myObservedLocalPlayerJoin = false;
        }

        public void OnPlayerJoined(Player player)
        {
            var apiUser = player.field_Private_APIUser_0;
            if (APIUser.CurrentUser.id == apiUser.id)
            {
                myObservedLocalPlayerJoin = true;
                myLastLevelLoad = Environment.TickCount;
            }

            if (!myObservedLocalPlayerJoin || Environment.TickCount - myLastLevelLoad < 5_000) return;
            if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
            var playerName = apiUser.displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(true))
                MelonCoroutines.Start(BlinkIconCoroutine(myJoinImage));
            if (JoinNotifierSettings.ShouldPlaySound(true))
               myJoinSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(true))
                MelonCoroutines.Start(ShowName(myJoinText, playerName));
        }
        
        public void OnPlayerLeft(Player player)
        {
            if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
            if (Environment.TickCount - myLastLevelLoad < 5_000) return;
            var playerName = player.field_Private_APIUser_0.displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(false))
                MelonCoroutines.Start(BlinkIconCoroutine(myLeaveImage));
            if (JoinNotifierSettings.ShouldPlaySound(false))
                myLeaveSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(false))
                MelonCoroutines.Start(ShowName(myLeaveText, playerName));
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