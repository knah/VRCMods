namespace UIExpansionKit.API
{
    /// <summary>
    /// This struct describes the required layout for a custom menu
    /// </summary>
    public struct LayoutDescription
    {
        /// <summary>
        /// Number of columns in grid layout of this menu. DOes not change menu width.
        /// </summary>
        public int NumColumns;
        
        /// <summary>
        /// Height of single row in menu-units
        /// </summary>
        public int RowHeight;

        /// <summary>
        /// Number of rows visible at a time. Adding more rows will enable scrolling. Affects menu height.
        /// </summary>
        public int NumRows;

        public static LayoutDescription QuickMenu3Columns = new LayoutDescription {NumColumns = 3, RowHeight = 380 / 3, NumRows = 3};
        public static LayoutDescription QuickMenu4Columns = new LayoutDescription {NumColumns = 4, RowHeight = 95, NumRows = 4 };
        public static LayoutDescription WideSlimList = new LayoutDescription {NumColumns = 1, RowHeight = 50, NumRows = 8 };
        
    }
}