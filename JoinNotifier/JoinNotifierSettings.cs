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
        private const string SettingFriendsJoinColor = "FriendJoinColor";
        private const string SettingFriendsLeaveColor = "FriendLeaveColor";
        
        private const string SettingUseUiMixer = "UseUiMixer";
        
        public static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(SettingsCategory, "Join Notifier");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldBlink, true, "Blink HUD icon on join");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldPlaySound, true, "Play sound on join");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingJoinShowName, true, "Show joined names");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveBlink, false, "Blink HUD icon on leave");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveSound, false, "Play sound on leave");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveShowName, false, "Show left names");
            
            ModPrefs.RegisterPrefFloat(SettingsCategory, SettingSoundVolume, .3f, "Sound volume (0-1)");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingUseUiMixer, true, "Notifications are affected by UI volume slider");
            ModPrefs.RegisterPrefInt(SettingsCategory, SettingTextSize, 36, "Text size (pt)");

            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPublic, false, "Notify in public instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyFriends, true, "Notify in friends[+] instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPrivate, true, "Notify in private instances");

            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShowFriendsOnly, false, "Show friend join/leave only");
            ModPrefs.RegisterPrefString(SettingsCategory, SettingJoinIconColor, "127 191 255", "Join icon color (r g b)");
            ModPrefs.RegisterPrefString(SettingsCategory, SettingLeaveIconColor, "153 82 51", "Leave icon color (r g b)");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShowFriendsInDifferentColor, true, "Show friend names in different color");
            ModPrefs.RegisterPrefString(SettingsCategory, SettingFriendsJoinColor, "224 224 0", "Friend join name color (r g b)");
            ModPrefs.RegisterPrefString(SettingsCategory, SettingFriendsLeaveColor, "201 201 0", "Friend leave name color (r g b)");
        }

        public static bool ShouldNotifyInCurrentInstance()
        {
            var instanceType = RoomManagerBase.field_Internal_Static_ApiWorldInstance_0?.InstanceType;
            if (instanceType == null) return false;    
            switch (instanceType)
            {
                case ApiWorldInstance.AccessType.Public:
                    return ModPrefs.GetBool(SettingsCategory, SettingNotifyPublic);
                case ApiWorldInstance.AccessType.FriendsOfGuests:
                case ApiWorldInstance.AccessType.FriendsOnly:
                    return ModPrefs.GetBool(SettingsCategory, SettingNotifyFriends);
                case ApiWorldInstance.AccessType.InviteOnly:
                case ApiWorldInstance.AccessType.InvitePlus:
                    return ModPrefs.GetBool(SettingsCategory, SettingNotifyPrivate);
                default:
                    return false;
            }
        }

        public static bool ShouldBlinkIcon(bool isJoin) =>
            ModPrefs.GetBool(SettingsCategory, isJoin ? SettingShouldBlink : SettingLeaveBlink);

        public static bool ShouldPlaySound(bool isJoin) =>
            ModPrefs.GetBool(SettingsCategory, isJoin ? SettingShouldPlaySound : SettingLeaveSound);
        
        public static bool ShouldShowNames(bool isJoin) =>
            ModPrefs.GetBool(SettingsCategory, isJoin ? SettingJoinShowName : SettingLeaveShowName);

        public static bool ShowFriendsOnly() => ModPrefs.GetBool(SettingsCategory, SettingShowFriendsOnly);
        public static bool ShowFriendsInDifferentColor() => ModPrefs.GetBool(SettingsCategory, SettingShowFriendsInDifferentColor);

        public static float GetSoundVolume() => ModPrefs.GetFloat(SettingsCategory, SettingSoundVolume);

        public static Color GetJoinIconColor() => DecodeColor(ModPrefs.GetString(SettingsCategory, SettingJoinIconColor));
        public static Color GetLeaveIconColor() => DecodeColor(ModPrefs.GetString(SettingsCategory, SettingLeaveIconColor));
        
        public static Color GetFriendJoinIconColor() => DecodeColor(ModPrefs.GetString(SettingsCategory, SettingFriendsJoinColor));
        public static Color GetFriendLeaveIconColor() => DecodeColor(ModPrefs.GetString(SettingsCategory, SettingFriendsLeaveColor));

        public static bool GetUseUiMixer() => ModPrefs.GetBool(SettingsCategory, SettingUseUiMixer);

        public static int GetTextSize() => ModPrefs.GetInt(SettingsCategory, SettingTextSize);

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