using MelonLoader;
using UnityEngine;
using VRC.Core;

namespace JoinNotifier
{
    public static class JoinNotifierSettings
    {
        private const string SettingsCategory = "JoinNotifier";
        private const string SettingShouldPlaySound = "PlaySound";
        private const string SettingSoundVolume = "SoundVolume";
        private const string SettingTextSize = "TextSize";
        private const string SettingShouldBlink = "BlinkIcon";
        private const string SettingJoinShowName = "ShowJoinedName";
        private const string SettingNotifyPublic = "NotifyInPublic";
        private const string SettingNotifyFriends = "NotifyInFriends";
        private const string SettingNotifyPrivate = "NotifyInPrivate";
        private const string SettingLeaveSound= "LeaveSound";
        private const string SettingLeaveBlink = "LeaveBlink";
        private const string SettingLeaveShowName = "ShowLeaveName";
        private const string SettingJoinIconColor = "JoinColor";
        private const string SettingLeaveIconColor = "LeaveColor";
        private const string SettingShowFriendsInDifferentColor = "ShowFriendsInDifferentColor";
        private const string SettingShowFriendsOnly = "ShowFriendsOnly";
        private const string SettingShowFriendsAlways = "ShowFriendsAlways";
        private const string SettingFriendsJoinColor = "FriendJoinColor";
        private const string SettingFriendsLeaveColor = "FriendLeaveColor";
        
        private const string SettingUseUiMixer = "UseUiMixer";
        
        public static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(SettingsCategory, "Join Notifier");
            
            MelonPrefs.RegisterBool(SettingsCategory, SettingShouldBlink, true, "Blink HUD icon on join");
            MelonPrefs.RegisterBool(SettingsCategory, SettingShouldPlaySound, true, "Play sound on join");
            MelonPrefs.RegisterBool(SettingsCategory, SettingJoinShowName, true, "Show joined names");
            
            MelonPrefs.RegisterBool(SettingsCategory, SettingLeaveBlink, false, "Blink HUD icon on leave");
            MelonPrefs.RegisterBool(SettingsCategory, SettingLeaveSound, false, "Play sound on leave");
            MelonPrefs.RegisterBool(SettingsCategory, SettingLeaveShowName, false, "Show left names");
            
            MelonPrefs.RegisterFloat(SettingsCategory, SettingSoundVolume, .3f, "Sound volume (0-1)");
            MelonPrefs.RegisterBool(SettingsCategory, SettingUseUiMixer, true, "Notifications are affected by UI volume slider");
            MelonPrefs.RegisterInt(SettingsCategory, SettingTextSize, 36, "Text size (pt)");

            MelonPrefs.RegisterBool(SettingsCategory, SettingNotifyPublic, false, "Notify in public instances");
            MelonPrefs.RegisterBool(SettingsCategory, SettingNotifyFriends, true, "Notify in friends[+] instances");
            MelonPrefs.RegisterBool(SettingsCategory, SettingNotifyPrivate, true, "Notify in private instances");

            MelonPrefs.RegisterBool(SettingsCategory, SettingShowFriendsOnly, false, "Show friend join/leave only");
            MelonPrefs.RegisterString(SettingsCategory, SettingJoinIconColor, "127 191 255", "Join icon color (r g b)");
            MelonPrefs.RegisterString(SettingsCategory, SettingLeaveIconColor, "153 82 51", "Leave icon color (r g b)");
            
            MelonPrefs.RegisterBool(SettingsCategory, SettingShowFriendsInDifferentColor, true, "Show friend names in different color");
            MelonPrefs.RegisterString(SettingsCategory, SettingFriendsJoinColor, "224 224 0", "Friend join name color (r g b)");
            MelonPrefs.RegisterString(SettingsCategory, SettingFriendsLeaveColor, "201 201 0", "Friend leave name color (r g b)");
            
            MelonPrefs.RegisterBool(SettingsCategory, SettingShowFriendsAlways, false, "Show friend join/leave regardless of instance type");
        }

        public static bool ShouldNotifyInCurrentInstance()
        {
            var instanceType = RoomManager.field_Internal_Static_ApiWorldInstance_0?.InstanceType;
            if (instanceType == null) return false;    
            switch (instanceType)
            {
                case ApiWorldInstance.AccessType.Public:
                    return MelonPrefs.GetBool(SettingsCategory, SettingNotifyPublic);
                case ApiWorldInstance.AccessType.FriendsOfGuests:
                case ApiWorldInstance.AccessType.FriendsOnly:
                    return MelonPrefs.GetBool(SettingsCategory, SettingNotifyFriends);
                case ApiWorldInstance.AccessType.InviteOnly:
                case ApiWorldInstance.AccessType.InvitePlus:
                    return MelonPrefs.GetBool(SettingsCategory, SettingNotifyPrivate);
                default:
                    return false;
            }
        }

        public static bool ShouldBlinkIcon(bool isJoin) =>
            MelonPrefs.GetBool(SettingsCategory, isJoin ? SettingShouldBlink : SettingLeaveBlink);

        public static bool ShouldPlaySound(bool isJoin) =>
            MelonPrefs.GetBool(SettingsCategory, isJoin ? SettingShouldPlaySound : SettingLeaveSound);
        
        public static bool ShouldShowNames(bool isJoin) =>
            MelonPrefs.GetBool(SettingsCategory, isJoin ? SettingJoinShowName : SettingLeaveShowName);

        public static bool ShowFriendsOnly() => MelonPrefs.GetBool(SettingsCategory, SettingShowFriendsOnly);
        public static bool ShowFriendsAlways() => MelonPrefs.GetBool(SettingsCategory, SettingShowFriendsAlways);
        public static bool ShowFriendsInDifferentColor() => MelonPrefs.GetBool(SettingsCategory, SettingShowFriendsInDifferentColor);

        public static float GetSoundVolume() => MelonPrefs.GetFloat(SettingsCategory, SettingSoundVolume);

        public static Color GetJoinIconColor() => DecodeColor(MelonPrefs.GetString(SettingsCategory, SettingJoinIconColor));
        public static Color GetLeaveIconColor() => DecodeColor(MelonPrefs.GetString(SettingsCategory, SettingLeaveIconColor));
        
        public static Color GetFriendJoinIconColor() => DecodeColor(MelonPrefs.GetString(SettingsCategory, SettingFriendsJoinColor));
        public static Color GetFriendLeaveIconColor() => DecodeColor(MelonPrefs.GetString(SettingsCategory, SettingFriendsLeaveColor));

        public static bool GetUseUiMixer() => MelonPrefs.GetBool(SettingsCategory, SettingUseUiMixer);

        public static int GetTextSize() => MelonPrefs.GetInt(SettingsCategory, SettingTextSize);

        private static Color DecodeColor(string color)
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
    }
}