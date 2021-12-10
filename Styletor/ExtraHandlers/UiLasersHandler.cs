using Styletor.Utils;
using UIExpansionKit;
using UnityEngine;
using VRC.UI;

namespace Styletor.ExtraHandlers
{
    public class UiLasersHandler
    {
        private readonly SpriteRenderer myMouseCursorSprite;
        private readonly SpriteRenderer myLeftDotSprite;
        private readonly SpriteRenderer myRightDotSprite;

        private readonly HandDotCursor myLeftHand;
        private readonly HandDotCursor myRightHand;

        private readonly SettingsHolder mySettings;

        private readonly Color myDefaultDotColor;
        private readonly Color myDefaultCursorColor;

        private readonly Color myDefaultLaserColorDark;
        private readonly Color myDefaultLaserColorBright;

        public UiLasersHandler(SettingsHolder settings)
        {
            mySettings = settings;
            
            myMouseCursorSprite = UnityUtils
                .FindInactiveObjectInActiveRoot("_Application/CursorManager/MouseArrow/VRCUICursorIcon")!
                .GetComponent<SpriteRenderer>();

            myDefaultCursorColor = myMouseCursorSprite.color;
            
            myLeftDotSprite = UnityUtils
                .FindInactiveObjectInActiveRoot("_Application/CursorManager/DotLeftHand/VRCUICursorIcon")!
                .GetComponent<SpriteRenderer>();

            myDefaultDotColor = myLeftDotSprite.color;
            
            myRightDotSprite = UnityUtils
                .FindInactiveObjectInActiveRoot("_Application/CursorManager/DotRightHand/VRCUICursorIcon")!
                .GetComponent<SpriteRenderer>();
            
            myLeftHand = UnityUtils
                .FindInactiveObjectInActiveRoot("_Application/CursorManager/DotLeftHand")!
                .GetComponent<HandDotCursor>();

            myDefaultLaserColorDark = myLeftHand.field_Public_Color_0;
            myDefaultLaserColorBright = myLeftHand.field_Public_Color_1;
            
            myRightHand = UnityUtils
                .FindInactiveObjectInActiveRoot("_Application/CursorManager/DotRightHand")!
                .GetComponent<HandDotCursor>();
            
            mySettings.RegisterUpdateDelegate(mySettings.UiLasersModeEntry, mySettings.UiLasersColorEntry, UpdateColors);
        }

        public void UpdateColors()
        {
            var color = mySettings.GetColorForMode(mySettings.UiLasersModeEntry, mySettings.UiLasersColorEntry);

            myLeftDotSprite.color = color ?? myDefaultDotColor;
            myRightDotSprite.color = color ?? myDefaultDotColor;
            myMouseCursorSprite.color = color ?? myDefaultCursorColor;

            var colorDarker = color?.RGBMultipliedClamped(0.5f);

            myLeftHand.field_Public_Color_0 = colorDarker ?? myDefaultLaserColorDark;
            myRightHand.field_Public_Color_0 = colorDarker ?? myDefaultLaserColorDark;

            myLeftHand.field_Public_Color_1 = color ?? myDefaultLaserColorBright;
            myRightHand.field_Public_Color_1 = color ?? myDefaultLaserColorBright;
        }
    }
}