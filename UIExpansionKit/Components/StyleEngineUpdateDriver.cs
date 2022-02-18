using System;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using VRC.UI.Core.Styles;

namespace UIExpansionKit.Components
{
    public class StyleEngineUpdateDriver : MonoBehaviour
    {
        public StyleEngine StyleEngine;
        private HashSet<StyleElement> mySet;

        public StyleEngineUpdateDriver(IntPtr ptr) : base(ptr)
        {
        }

        private void Start()
        {
            mySet = StyleEngine.field_Private_HashSet_1_StyleElement_0;
        }

        private void LateUpdate()
        {
            if (mySet._count > 0)
                StyleEngine.LateUpdate();
        }
    }
}