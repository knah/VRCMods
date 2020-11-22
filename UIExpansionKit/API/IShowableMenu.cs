namespace UIExpansionKit.API
{
    public interface IShowableMenu
    {
        /// <summary>
        /// Shows this menu on top of parent menu.
        /// Requires the parent menu to be open.
        /// </summary>
        /// <param name="onTop">If another custom menu(s) is/are already showing, and this is false, the other menu(s) will be hidden; otherwise, this menu will be shown on top of it/them</param>
        void Show(bool onTop = false);
        
        /// <summary>
        /// Hides this menu if it's showing. If there are menus showing on top of it, they will be hidden too.
        /// </summary>
        void Hide();
    }
}