using UIExpansionKit.API.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit.ControlsImpl
{
    public class MenuToggle : MenuControlWithText, IMenuToggle
    {
        private Toggle myToggle;
        private bool myInitialIsSelected;

        public override void ConsumeGameObject(GameObject obj)
        {
            myToggle = obj.GetComponentInChildren<Toggle>(true);
            myToggle.isOn = myInitialIsSelected;
            
            base.ConsumeGameObject(obj);
        }

        public MenuToggle(string text, TextAnchor anchor, bool initialIsSelected) : base(text, anchor)
        {
            myInitialIsSelected = initialIsSelected;
        }

        public bool Selected
        {
            get => myToggle == null ? myInitialIsSelected : myToggle.isOn;
            set
            {
                if (myToggle == null)
                    myInitialIsSelected = value;
                else if (myToggle.isOn != value) myToggle.isOn = value;
            }
        }
    }
}