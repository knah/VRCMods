using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JoinNotifier;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(JoinNotifierMod), "JoinNotifier", JoinNotifierMod.VersionConst, "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace JoinNotifier
{
    public class JoinNotifierMod : MelonMod
    {
        public const string VersionConst = "0.2.8";
        private const string CustomJoinSoundFileName = "UserData/JN-Join.ogg";
        private const string CustomLeaveSoundFileName = "UserData/JN-Leave.ogg";

        private readonly List<string> myJoinNames = new List<string>();
        private readonly List<string> myLeaveNames = new List<string>();

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

        private AudioMixerGroup myUIGroup;

        public override void OnApplicationStart()
        {
            JoinNotifierSettings.RegisterSettings();

            MelonCoroutines.Start(InitThings());
        }

        public IEnumerator InitThings()
        {
            MelonLogger.Log("Waiting for init");
            
            while (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null)) yield return null;
            while (ReferenceEquals(VRCAudioManager.field_Private_Static_VRCAudioManager_0, null)) yield return null;
            while (ReferenceEquals(VRCUiManager.prop_VRCUiManager_0, null)) yield return null;

            var audioManager = VRCAudioManager.field_Private_Static_VRCAudioManager_0;

            myUIGroup = new[]
            {
                audioManager.field_Public_AudioMixerGroup_0, audioManager.field_Public_AudioMixerGroup_1,
                audioManager.field_Public_AudioMixerGroup_2
            }.Single(it => it.name == "UI");

            MelonLogger.Log("Start init");
            
            NetworkManagerHooks.Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JoinNotifier.joinnotifier.assetbundle"))
            using (var tempStream = new MemoryStream((int) stream.Length))
            {
                stream.CopyTo(tempStream);
                
                myAssetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                myAssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }
            
            myJoinSprite = myAssetBundle.LoadAsset_Internal("Assets/JoinNotifier/JoinIcon.png", Il2CppType.Of<Sprite>()).Cast<Sprite>();
            myJoinSprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            if (File.Exists(CustomJoinSoundFileName))
            {
                MelonLogger.Msg("Loading custom join sound");
                var www = new WWW($"file://{Path.Combine(Environment.CurrentDirectory, CustomJoinSoundFileName)}");
                
                while (www.keepWaiting) yield return null;
                
                myJoinClip = www.GetAudioClip();
            }
            
            if (myJoinClip == null)
                myJoinClip = myAssetBundle.LoadAsset_Internal("Assets/JoinNotifier/Chime.ogg", Il2CppType.Of<AudioClip>()).Cast<AudioClip>();
            
            myJoinClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            if (File.Exists(CustomLeaveSoundFileName))
            {
                MelonLogger.Msg("Loading custom leave sound");
                var www = new WWW($"file://{Path.Combine(Environment.CurrentDirectory, CustomLeaveSoundFileName)}");
                while (www.keepWaiting) yield return null;
                
                myLeaveClip = www.GetAudioClip();
            }
            
            if (myLeaveClip == null)
                myLeaveClip = myAssetBundle.LoadAsset_Internal("Assets/JoinNotifier/DoorClose.ogg", Il2CppType.Of<AudioClip>()).Cast<AudioClip>();
            
            myLeaveClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            CreateGameObjects();
            
            NetworkManagerHooks.OnJoin += OnPlayerJoined;
            NetworkManagerHooks.OnLeave += OnPlayerLeft;
        }

        public override void OnModSettingsApplied()
        {
            MelonLogger.Log("Settings apply start");
            if (myJoinSource != null)
            {
                myJoinSource.volume = JoinNotifierSettings.GetSoundVolume();
                myJoinSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? myUIGroup : null;
            }

            if (myLeaveSource != null)
            {
                myLeaveSource.volume = JoinNotifierSettings.GetSoundVolume();
                myLeaveSource.outputAudioMixerGroup = JoinNotifierSettings.GetUseUiMixer() ? myUIGroup : null;
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
            MelonLogger.Log("Settings apply done");
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
            text.color = UnityEngine.Color.white;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = alignment;
            text.fontSize = JoinNotifierSettings.GetTextSize();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.supportRichText = true;

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
                source.outputAudioMixerGroup = myUIGroup;
            return source;
        }

        private void CreateGameObjects()
        {
            if (myJoinImage != null) return;

            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            if (hudRoot == null)
            {
                MelonLogger.Log("Not creating gameobjects - no hud root");
                return;
            }
            
            MelonLogger.Log("Creating gameobjects");
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
            // MelonLogger.Log("Scene load");
            
            myLastLevelLoad = Environment.TickCount;
            myObservedLocalPlayerJoin = false;
        }

        public void OnPlayerJoined(Player player)
        {
            var apiUser = player?.field_Private_APIUser_0;
            if (apiUser == null) return;
            if (APIUser.CurrentUser.id == apiUser.id)
            {
                myObservedLocalPlayerJoin = true;
                myLastLevelLoad = Environment.TickCount;
            }

            if (!myObservedLocalPlayerJoin || Environment.TickCount - myLastLevelLoad < 5_000) return;
            var isFriendsWith = APIUser.IsFriendsWith(apiUser.id);
            if (!isFriendsWith || !JoinNotifierSettings.ShowFriendsAlways())
            {
                if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
                if (JoinNotifierSettings.ShowFriendsOnly() && !isFriendsWith) return;
            }
            var playerName = apiUser.displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(true))
                MelonCoroutines.Start(BlinkIconCoroutine(myJoinImage));
            if (JoinNotifierSettings.ShouldPlaySound(true))
               myJoinSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(true))
                MelonCoroutines.Start(ShowName(myJoinText, myJoinNames, playerName, true, isFriendsWith));
        }
        
        public void OnPlayerLeft(Player player)
        {
            var apiUser = player?.field_Private_APIUser_0;
            if (apiUser == null) return;
            if (Environment.TickCount - myLastLevelLoad < 5_000) return;

            var isFriendsWith = APIUser.IsFriendsWith(apiUser.id);
            if (!isFriendsWith || !JoinNotifierSettings.ShowFriendsAlways())
            {
                if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
                if (JoinNotifierSettings.ShowFriendsOnly() && !isFriendsWith) return;
            }

            var playerName = apiUser.displayName ?? "!null!";
            if (JoinNotifierSettings.ShouldBlinkIcon(false))
                MelonCoroutines.Start(BlinkIconCoroutine(myLeaveImage));
            if (JoinNotifierSettings.ShouldPlaySound(false))
                myLeaveSource.Play();
            if (JoinNotifierSettings.ShouldShowNames(false))
                MelonCoroutines.Start(ShowName(myLeaveText, myLeaveNames, playerName, false, isFriendsWith));
        }

        public IEnumerator ShowName(Text text, List<string> namesList, string name, bool isJoin, bool isFriend)
        {
            var color = JoinNotifierSettings.ShowFriendsInDifferentColor() && isFriend
                ? (isJoin
                    ? JoinNotifierSettings.GetFriendJoinIconColor()
                    : JoinNotifierSettings.GetFriendLeaveIconColor())
                : (isJoin ? JoinNotifierSettings.GetJoinIconColor() : JoinNotifierSettings.GetLeaveIconColor());
            var playerLine = $"<color={RenderHex(color)}>{name}</color>";

            namesList.Add(playerLine);
            
            text.text = string.Join("\n", namesList);
            yield return new WaitForSeconds(3);
            namesList.Remove(playerLine);
            text.text = string.Join("\n", namesList);
        }

        private static string RenderHex(Color color)
        {
            return $"#{(int) (color.r * 255):X2}{(int) (color.g * 255):X2}{(int) (color.b * 255):X2}{(int) (color.a * 255):X2}";
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
