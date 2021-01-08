using System;
using UnityEngine;

namespace AdvancedSafety
{
    public class QuickMenuHideAvatarButtonHandler : MonoBehaviour
    {
        private QuickMenu myQuickMenu;
        private float myTimeAccumulator;
        
        public QuickMenuHideAvatarButtonHandler(IntPtr ptr) : base(ptr)
        {
        }

        private void Awake()
        {
            myQuickMenu = QuickMenu.prop_QuickMenu_0;
        }

        private void Update()
        {
            myTimeAccumulator += Time.deltaTime;
            if (myTimeAccumulator < .5f) return;

            myTimeAccumulator = 0;
            
            var player = myQuickMenu.field_Private_Player_0;
            if (player == null) return;
            var vrcPlayer = player.prop_VRCPlayer_0;
            if (vrcPlayer == null) return;

            UiExpansionKitSupport.QuickMenuUpdateTick(vrcPlayer);
        }
    }
}