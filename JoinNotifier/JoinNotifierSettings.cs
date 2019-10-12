using UnityEngine;
using VRC.Core;
using VRCTools;

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
        private const string SettingJoinIconColor = "JoinIconColor";
        private const string SettingLeaveIconColor = "LeaveIconColor";
        
        private const string SettingUseUiMixer = "UseUiMixer";
        
        public static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(SettingsCategory, "Join Notifier");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldBlink, true, "Blink HUD icon on join");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldPlaySound, true, "Play sound on join");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingJoinShowName, true, "Show joined names");
            
            ModPrefs.RegisterPrefFloat(SettingsCategory, SettingSoundVolume, .3f, "Sound volume (0-1)");
            ModPrefs.RegisterPrefInt(SettingsCategory, SettingTextSize, 36, "Text size (pt)");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPublic, false, "Notify in public instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyFriends, true, "Notify in friends[+] instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPrivate, true, "Notify in private instances");
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveBlink, false, "Blink HUD icon on leave");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveSound, false, "Play sound on leave");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingLeaveShowName, false, "Show left names");
            
            ModPrefs.RegisterPrefColor(SettingsCategory, SettingJoinIconColor, new Color(0.50F, 0.75F, 1F), hideFromList: true);
            ModPrefs.RegisterPrefColor(SettingsCategory, SettingLeaveIconColor, new Color(0.6f, 0.32f, 0.2f), hideFromList: true);
            
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingUseUiMixer, true, "Notifications are UI sounds", hideFromList: true);
        }

        public static bool ShouldNotifyInCurrentInstance()
        {
            var instanceType = RoomManagerBase.currentWorldInstance?.InstanceType;
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

        public static float GetSoundVolume() => ModPrefs.GetFloat(SettingsCategory, SettingSoundVolume);

        public static Color GetJoinIconColor() => ModPrefs.GetColor(SettingsCategory, SettingJoinIconColor);
        public static Color GetLeaveIconColor() => ModPrefs.GetColor(SettingsCategory, SettingLeaveIconColor);

        public static bool GetUseUiMixer() => ModPrefs.GetBool(SettingsCategory, SettingUseUiMixer);

        public static int GetTextSize() => ModPrefs.GetInt(SettingsCategory, SettingTextSize);
    }
}