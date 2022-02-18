using System;
using TMPro;
using UIExpansionKit.API.Controls;
using UnityEngine;
using UnityEngine.UI;

namespace UIExpansionKit.ControlsImpl
{
    public class MenuControlWithText : BaseMenuControl, IMenuControlWithText
    {
        private string myText;
        private TMP_Text myTextComponent;
        private TextAnchor myAnchor;

        public MenuControlWithText(string text, TextAnchor anchor)
        {
            myText = text;
            myAnchor = anchor;
        }

        private TextAlignmentOptions ConvertAnchor() => myAnchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        private TextAnchor UnConvertAnchor(TextAlignmentOptions alignment) => alignment switch
        {
            TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
            TextAlignmentOptions.Top => TextAnchor.UpperCenter,
            TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
            TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
            TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
            TextAlignmentOptions.Right => TextAnchor.MiddleRight,
            TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft,
            TextAlignmentOptions.Bottom => TextAnchor.LowerCenter,
            TextAlignmentOptions.BottomRight => TextAnchor.LowerRight,
            _ => throw new ArgumentOutOfRangeException()
        };

        public string Text
        {
            get => myTextComponent != null ? myTextComponent.text : myText;
            set
            {
                myText = value;
                if (myTextComponent != null) 
                    myTextComponent.text = value;
            }
        }

        public TextAnchor Anchor
        {
            get => myTextComponent == null ? UnConvertAnchor(myTextComponent.alignment) : myAnchor;
            set
            {
                myAnchor = value;
                if (myTextComponent != null) 
                    myTextComponent.alignment = ConvertAnchor();
            }
        }

        public override void ConsumeGameObject(GameObject obj)
        {
            myTextComponent = obj.GetComponentInChildren<TMP_Text>(true);
            myTextComponent.text = myText;
            myTextComponent.alignment = ConvertAnchor();

            var legacyTextComponent = obj.GetComponentInChildren<Text>(true);
            legacyTextComponent.text = myText; // set this for the copying code
            
            base.ConsumeGameObject(obj);
        }
    }
}