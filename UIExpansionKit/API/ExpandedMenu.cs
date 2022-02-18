using System;

namespace UIExpansionKit.API
{
    /// <summary>
    /// Menus supported by UI Expansion Kit
    /// </summary>
    public enum ExpandedMenu
    {
        /// <summary>
        /// Normal QuickMenu (nobody selected)
        /// </summary>
        QuickMenu,
        
        /// <summary>
        /// QuickMenu with a user (in instance) selected
        /// </summary>
        UserQuickMenu,
        
        /// <summary>
        /// Big avatar menu
        /// </summary>
        AvatarMenu,
        
        /// <summary>
        /// Big list of worlds menu
        /// </summary>
        WorldMenu,
        
        /// <summary>
        /// Big world details menu
        /// </summary>
        WorldDetailsMenu,

        /// <summary>
        /// Big user details menu
        /// </summary>
        UserDetailsMenu,
        
        /// <summary>
        /// Big social menu
        /// </summary>
        SocialMenu,
        
        /// <summary>
        /// Big settings menu. This constant indicates the usual sidebar of big menu, not Mod Settings popup
        /// </summary>
        SettingsMenu,
        
        /// <summary>
        /// Big safety menu
        /// </summary>
        SafetyMenu,
        
        /// <summary>
        /// Here Quick Menu tab
        /// </summary>
        QuickMenuHere,
        
        [Obsolete("There is no Emote page in Quick Menu anymore; buttons added here will end up on the Here page")]
        EmoteQuickMenu = QuickMenuHere,
        
        /// <summary>
        /// Emoji Quick Menu page
        /// </summary>
        EmojiQuickMenu,
        
        /// <summary>
        /// Camera Quick Menu page
        /// </summary>
        CameraQuickMenu,
        
        /// <summary>
        /// UI Elements Quick Menu page
        /// </summary>
        UiElementsQuickMenu,
        
        /// <summary>
        /// Quick Menu Audio Settings tab
        /// </summary>
        QuickMenuAudioSettings,
        
        [Obsolete("There is no Moderation page in Quick Menu anymore; buttons added here will end up on the Here page")]
        ModerationQuickMenu = QuickMenuAudioSettings,
        
        /// <summary>
        /// Quick Menu avatar stats/info page
        /// </summary>
        AvatarStatsQuickMenu,
        
        /// <summary>
        /// The invites tab in quick menu
        /// </summary>
        InvitesTab,
        
        /// <summary>
        /// The handheld camera object
        /// </summary>
        Camera,
        
        /// <summary>
        /// The user selection quick menu for not-in-instance users
        /// </summary>
        UserQuickMenuRemote,
    }
}