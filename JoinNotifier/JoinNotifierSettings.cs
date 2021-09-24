using MelonLoader;
using UnityEngine;
using VRC.Core;

namespace JoinNotifier
{
    public static class JoinNotifierSettings
    {
        internal static MelonPreferences_Entry<bool> ShouldJoinBlink;
        internal static MelonPreferences_Entry<bool> ShouldPlayJoinSound;
        internal static MelonPreferences_Entry<bool> JoinShowName;
        
        internal static MelonPreferences_Entry<bool> ShouldLeaveBlink;
        internal static MelonPreferences_Entry<bool> ShouldPlayLeaveSound;
        internal static MelonPreferences_Entry<bool> LeaveShowName;
        
        
        internal static MelonPreferences_Entry<float> SoundVolume;
        internal static MelonPreferences_Entry<bool> UseUiMixer;
        internal static MelonPreferences_Entry<int> TextSize;
        
        internal static MelonPreferences_Entry<bool> NotifyInPublic;
        internal static MelonPreferences_Entry<bool> NotifyInFriends;
        internal static MelonPreferences_Entry<bool> NotifyInPrivate;
        
        internal static MelonPreferences_Entry<bool> ShowFriendsOnly;
        internal static MelonPreferences_Entry<string> JoinIconColor;
        internal static MelonPreferences_Entry<string> LeaveIconColor;
        
        internal static MelonPreferences_Entry<bool> ShowFriendsInDifferentColor;
        internal static MelonPreferences_Entry<string> FriendsJoinIconColor;
        internal static MelonPreferences_Entry<string> FriendsLeaveIconColor;
        
        internal static MelonPreferences_Entry<bool> ShowFriendsAlways;
        internal static MelonPreferences_Entry<bool> HideBlockedUsers;

        internal static MelonPreferences_Entry<bool> LogJoinsLeavesToConsole;

        public static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory("JoinNotifier", "Join Notifier");
            
            ShouldJoinBlink = category.CreateEntry("BlinkIcon", true, "Blink HUD icon on join");
            ShouldPlayJoinSound = category.CreateEntry("PlaySound", true, "Play sound on join");
            JoinShowName = category.CreateEntry("ShowJoinedName", true, "Show joined names");
            
            ShouldLeaveBlink = category.CreateEntry("LeaveBlink", false, "Blink HUD icon on leave");
            ShouldPlayLeaveSound = category.CreateEntry("LeaveSound", false, "Play sound on leave");
            LeaveShowName = category.CreateEntry("ShowLeaveName", false, "Show left names");
            
            SoundVolume = category.CreateEntry("SoundVolume", .3f, "Sound volume (0-1)");
            UseUiMixer = category.CreateEntry("UseUiMixer", true, "Notifications are affected by UI volume slider");
            TextSize = category.CreateEntry("TextSize", 36, "Text size (pt)");
            
            NotifyInPublic = category.CreateEntry("NotifyInPublic", false, "Notify in public instances");
            NotifyInFriends = category.CreateEntry("NotifyInFriends", true, "Notify in friends[+] instances");
            NotifyInPrivate = category.CreateEntry("NotifyInPrivate", true, "Notify in private instances");
            
            ShowFriendsOnly = category.CreateEntry("ShowFriendsOnly", false, "Show friend join/leave only");
            JoinIconColor = category.CreateEntry("JoinColor", "127 191 255", "Join icon color (r g b)");
            LeaveIconColor = category.CreateEntry("LeaveColor", "153 82 51", "Leave icon color (r g b)");
            
            ShowFriendsInDifferentColor = category.CreateEntry("ShowFriendsInDifferentColor", true, "Show friend names in different color");
            FriendsJoinIconColor = category.CreateEntry("FriendJoinColor", "224 224 0", "Friend join name color (r g b)");
            FriendsLeaveIconColor = category.CreateEntry("FriendLeaveColor", "201 201 0", "Friend leave name color (r g b)");
            
            ShowFriendsAlways = category.CreateEntry("ShowFriendsAlways", false, "Show friend join/leave regardless of instance type");
            HideBlockedUsers = category.CreateEntry("HideBlockedUsers", false, "Hide join/leave of blocked users");

            LogJoinsLeavesToConsole = category.CreateEntry("LogToConsole", false, "Log join/leave to MelonLoader console");
        }

        public static bool ShouldNotifyInCurrentInstance()
        {
            var instanceType = RoomManager.field_Internal_Static_ApiWorldInstance_0?.type;
            if (instanceType == null) return false;    
            switch (instanceType)
            {
                case InstanceAccessType.Public:
                    return NotifyInPublic.Value;
                case InstanceAccessType.FriendsOfGuests:
                case InstanceAccessType.FriendsOnly:
                    return NotifyInFriends.Value;
                case InstanceAccessType.InviteOnly:
                case InstanceAccessType.InvitePlus:
                    return NotifyInPrivate.Value;
                default:
                    return false;
            }
        }

        public static bool ShouldBlinkIcon(bool isJoin) => isJoin ? ShouldJoinBlink.Value : ShouldLeaveBlink.Value;
        
        public static bool ShouldPlaySound(bool isJoin) =>isJoin ? ShouldPlayJoinSound.Value : ShouldPlayLeaveSound.Value;

        public static bool ShouldShowNames(bool isJoin) => isJoin ? JoinShowName.Value : LeaveShowName.Value;


        public static Color GetJoinIconColor() => DecodeColor(JoinIconColor.Value);
        public static Color GetLeaveIconColor() => DecodeColor(LeaveIconColor.Value);
        
        public static Color GetFriendJoinIconColor() => DecodeColor(FriendsJoinIconColor.Value);
        public static Color GetFriendLeaveIconColor() => DecodeColor(FriendsLeaveIconColor.Value);

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