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
using UnityEngine.Networking;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.Management;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(JoinNotifierMod), "JoinNotifier", "1.0.5", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace JoinNotifier
{
    internal partial class JoinNotifierMod : MelonMod
    {
        private const string CustomJoinSoundFileName = "UserData/JN-Join.ogg";
        private const string CustomLeaveSoundFileName = "UserData/JN-Leave.ogg";

        private readonly List<string> myJoinNames = new();
        private readonly List<string> myLeaveNames = new();

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
            if (!CheckWasSuccessful || !MustStayTrue || MustStayFalse) return;
            
            JoinNotifierSettings.RegisterSettings();

            MelonCoroutines.Start(InitThings());
        }

        public IEnumerator InitThings()
        {
            MelonDebug.Msg("Waiting for init");
            
            while (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null)) yield return null;
            while (ReferenceEquals(VRCAudioManager.field_Private_Static_VRCAudioManager_0, null)) yield return null;
            while (ReferenceEquals(GetUiManager(), null)) yield return null;

            var audioManager = VRCAudioManager.field_Private_Static_VRCAudioManager_0;

            myUIGroup = new[]
            {
                audioManager.field_Public_AudioMixerGroup_0, audioManager.field_Public_AudioMixerGroup_1,
                audioManager.field_Public_AudioMixerGroup_2
            }.Single(it => it.name == "UI");

            MelonDebug.Msg("Start init");
            
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
                var uwr = UnityWebRequest.Get($"file://{Path.Combine(Environment.CurrentDirectory, CustomJoinSoundFileName)}");
                uwr.SendWebRequest();

                while (!uwr.isDone) yield return null;
                
                myJoinClip = WebRequestWWW.InternalCreateAudioClipUsingDH(uwr.downloadHandler, uwr.url, false, false, AudioType.UNKNOWN);
            }
            
            if (myJoinClip == null)
                myJoinClip = myAssetBundle.LoadAsset_Internal("Assets/JoinNotifier/Chime.ogg", Il2CppType.Of<AudioClip>()).Cast<AudioClip>();
            
            myJoinClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            if (File.Exists(CustomLeaveSoundFileName))
            {
                MelonLogger.Msg("Loading custom leave sound");
                
                var uwr = UnityWebRequest.Get($"file://{Path.Combine(Environment.CurrentDirectory, CustomLeaveSoundFileName)}");
                uwr.SendWebRequest();

                while (!uwr.isDone) yield return null;
                
                myLeaveClip = WebRequestWWW.InternalCreateAudioClipUsingDH(uwr.downloadHandler, uwr.url, false, false, AudioType.UNKNOWN);
            }
            
            if (myLeaveClip == null)
                myLeaveClip = myAssetBundle.LoadAsset_Internal("Assets/JoinNotifier/DoorClose.ogg", Il2CppType.Of<AudioClip>()).Cast<AudioClip>();
            
            myLeaveClip.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            CreateGameObjects();
            
            NetworkManagerHooks.OnJoin += OnPlayerJoined;
            NetworkManagerHooks.OnLeave += OnPlayerLeft;

            JoinNotifierSettings.SoundVolume.OnValueChanged += (_, _) => ApplySoundSettings();
            JoinNotifierSettings.UseUiMixer.OnValueChanged += (_, _) => ApplySoundSettings();
            JoinNotifierSettings.TextSize.OnValueChanged += (_, _) => ApplyFontSize();
            JoinNotifierSettings.JoinIconColor.OnValueChanged += (_, _) => {
                if (myJoinImage != null) myJoinImage.color = JoinNotifierSettings.GetJoinIconColor();
            };
            JoinNotifierSettings.LeaveIconColor.OnValueChanged += (_, _) =>
            {
                if (myLeaveImage != null) myLeaveImage.color = JoinNotifierSettings.GetLeaveIconColor();
            };
        }

        private void ApplySoundSettings()
        {
            if (myJoinSource != null)
            {
                myJoinSource.volume = JoinNotifierSettings.SoundVolume.Value;
                myJoinSource.outputAudioMixerGroup = JoinNotifierSettings.UseUiMixer.Value ? myUIGroup : null;
            }

            if (myLeaveSource != null)
            {
                myLeaveSource.volume = JoinNotifierSettings.SoundVolume.Value;
                myLeaveSource.outputAudioMixerGroup = JoinNotifierSettings.UseUiMixer.Value ? myUIGroup : null;
            }
        }

        private void ApplyFontSize()
        {
            if (myJoinText != null) myJoinText.fontSize = JoinNotifierSettings.TextSize.Value;
            if (myLeaveText != null) myLeaveText.fontSize = JoinNotifierSettings.TextSize.Value;
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
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = alignment;
            text.fontSize = JoinNotifierSettings.TextSize.Value;
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
            source.volume = JoinNotifierSettings.SoundVolume.Value;
            source.loop = false;
            source.playOnAwake = false;
            if (JoinNotifierSettings.UseUiMixer.Value)
                source.outputAudioMixerGroup = myUIGroup;
            return source;
        }

        private void CreateGameObjects()
        {
            if (myJoinImage != null) return;

            var hudRoot = GameObject.Find("UserInterface/UnscaledUI/HudContent/Hud");
            if (hudRoot == null)
            {
                MelonLogger.Msg("Not creating gameobjects - no hud root");
                return;
            }
            
            MelonDebug.Msg("Creating gameobjects");
//            var pathToThing = "UserInterface/UnscaledUI/HudContent/Hud/NotificationDotParent/NotificationDot";
            myJoinImage = CreateNotifierImage("join", 0f, JoinNotifierSettings.GetJoinIconColor());
            myJoinSource = CreateAudioSource(myJoinClip, myJoinImage.gameObject);
            myJoinText = CreateTextNear(myJoinImage, 110f, TextAnchor.LowerRight);
            
            myLeaveImage = CreateNotifierImage("leave", 100f, JoinNotifierSettings.GetLeaveIconColor());
            myLeaveSource = CreateAudioSource(myLeaveClip, myLeaveImage.gameObject);
            myLeaveText = CreateTextNear(myLeaveImage, 110f, TextAnchor.LowerLeft);
        }

        partial void OnSceneWasLoaded2(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            
            myLastLevelLoad = Environment.TickCount;
            myObservedLocalPlayerJoin = false;
        }

        public void OnPlayerJoined(Player player)
        {
            var apiUser = player.prop_APIUser_0;
            if (apiUser == null) return;
            if (APIUser.CurrentUser.id == apiUser.id)
            {
                myObservedLocalPlayerJoin = true;
                myLastLevelLoad = Environment.TickCount;
            }

            if (!myObservedLocalPlayerJoin || Environment.TickCount - myLastLevelLoad < 5_000) return;
            
            if (JoinNotifierSettings.HideBlockedUsers.Value && IsBlocked(apiUser.id)) return;
            
            var isFriendsWith = APIUser.IsFriendsWith(apiUser.id);
            if (!isFriendsWith || !JoinNotifierSettings.ShowFriendsAlways.Value)
            {
                if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
                if (JoinNotifierSettings.ShowFriendsOnly.Value && !isFriendsWith) return;
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
            var apiUser = player.prop_APIUser_0;
            if (apiUser == null) return;
            if (Environment.TickCount - myLastLevelLoad < 5_000) return;

            if (JoinNotifierSettings.HideBlockedUsers.Value && IsBlocked(apiUser.id)) return;

            var isFriendsWith = APIUser.IsFriendsWith(apiUser.id);
            if (!isFriendsWith || !JoinNotifierSettings.ShowFriendsAlways.Value)
            {
                if (!JoinNotifierSettings.ShouldNotifyInCurrentInstance()) return;
                if (JoinNotifierSettings.ShowFriendsOnly.Value && !isFriendsWith) return;
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
            var color = JoinNotifierSettings.ShowFriendsInDifferentColor.Value && isFriend
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
        
        private static bool IsBlocked(string userId)
        {
            if (userId == null) return false;
            
            var moderationManager = ModerationManager.prop_ModerationManager_0;
            if (moderationManager == null) return false;
            if (APIUser.CurrentUser?.id == userId)
                return false;
            
            var moderationsDict = ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0;
            if (!moderationsDict.ContainsKey(userId)) return false;
            
            foreach (var playerModeration in moderationsDict[userId])
            {
                if (playerModeration != null && playerModeration.moderationType == ApiPlayerModeration.ModerationType.Block)
                    return true;
            }
            
            return false;
            
        }
    }
}
