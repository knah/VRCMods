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
        /// QuickMenu with a user selected
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
        /// Emote Quick Menu page
        /// </summary>
        EmoteQuickMenu,
        
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
        /// Moderation Quick Menu page
        /// </summary>
        ModerationQuickMenu,
        
        /// <summary>
        /// Avatar stats Quick Menu page
        /// </summary>
        AvatarStatsQuickMenu,
    }
}