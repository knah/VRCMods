using System;
using UIExpansionKit;
using UnityEngine;

namespace ParticleAndBoneLimiterSettings
{
    public class CustomParticleSettingsUiHandler : MonoBehaviour
    {
        internal static PreloadedBundleContents UixBundle;
        
        public CustomParticleSettingsUiHandler(IntPtr ptr) : base(ptr)
        {
        }

        private void Awake()
        {
            ParticleAndBoneLimiterSettingsMod.InitializeSettingsCategory(gameObject);
        }
    }
}