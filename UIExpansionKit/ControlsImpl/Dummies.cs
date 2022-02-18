using UIExpansionKit.API.Controls;
using UnityEngine;

namespace UIExpansionKit.ControlsImpl
{
    public class DummyControlWithText : MenuControlWithText, IMenuButton, IMenuLabel
    {
        public DummyControlWithText(string text, TextAnchor anchor) : base(text, anchor)
        {
        }
    }
}