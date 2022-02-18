using UnityEngine;

namespace UIExpansionKit.API.Controls
{
    public static class FluentExtensions
    {
        public static T SetVisible<T>(this T @this, bool visible) where T : IMenuControl
        {
            @this.Visible = visible;
            return @this;
        }
        
        public static T SetText<T>(this T @this, string text) where T : IMenuControlWithText
        {
            @this.Text = text;
            return @this;
        }
        
        public static T SetAnchor<T>(this T @this, TextAnchor anchor) where T : IMenuControlWithText
        {
            @this.Anchor = anchor;
            return @this;
        }
        
        public static T SetSelected<T>(this T @this, bool selected) where T : IMenuToggle
        {
            @this.Selected = selected;
            return @this;
        }
    }
}