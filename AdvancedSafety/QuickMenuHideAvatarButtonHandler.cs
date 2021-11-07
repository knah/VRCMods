using System;
using UIExpansionKit;
using UnityEngine;

namespace AdvancedSafety
{
    public class QuickMenuHideAvatarButtonHandler : MonoBehaviour
    {
        private float myTimeAccumulator;
        
        public QuickMenuHideAvatarButtonHandler(IntPtr ptr) : base(ptr)
        {
        }

        private void Update()
        {
            myTimeAccumulator += Time.deltaTime;
            if (myTimeAccumulator < .5f) return;

            myTimeAccumulator = 0;
            
            var player = UiExpansionKitSupport.GetUserSelectedInQm()?.GetPlayer();
            if (player == null) return;
            var vrcPlayer = player.prop_VRCPlayer_0;
            if (vrcPlayer == null) return;

            UiExpansionKitSupport.QuickMenuUpdateTick(vrcPlayer);
        }
    }
}