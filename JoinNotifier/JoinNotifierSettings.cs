using System;
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
        private const string SettingShouldBlink = "BlinkIcon";
        private const string SettingNotifyPublic = "NotifyInPublic";
        private const string SettingNotifyFriends = "NotifyInFriends";
        private const string SettingNotifyPrivate = "NotifyInPrivate";
        
        public static void RegisterSettings()
        {
            ModPrefs.RegisterCategory(SettingsCategory, "Join Notifier");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldBlink, true, "Blink HUD icon");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingShouldPlaySound, true, "Play sound");
            ModPrefs.RegisterPrefFloat(SettingsCategory, SettingSoundVolume, .3f, "Sound volume (0-1)");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPublic, false, "Notify in public instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyFriends, true, "Notify in friends[+] instances");
            ModPrefs.RegisterPrefBool(SettingsCategory, SettingNotifyPrivate, true, "Notify in private instances");
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

        public static bool ShouldBlinkIcon()
        {
            return ModPrefs.GetBool(SettingsCategory, SettingShouldBlink);
        }

        public static bool ShouldPlaySound()
        {
            return ModPrefs.GetBool(SettingsCategory, SettingShouldPlaySound);
        }

        public static float GetSoundVolume()
        {
            return ModPrefs.GetFloat(SettingsCategory, SettingSoundVolume);
        }
    }
}